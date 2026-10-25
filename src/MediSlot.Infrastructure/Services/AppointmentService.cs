using System.Linq.Expressions;
using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Appointments;
using MediSlot.Application.Interfaces;
using MediSlot.Domain.Entities;
using MediSlot.Domain.Enums;
using MediSlot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MediSlot.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _db;
    private readonly TimeZoneInfo _tz;
    private readonly int _cancellationCutoffHours;

    public AppointmentService(AppDbContext db, IOptions<ClinicSettings> options)
    {
        _db = db;
        _tz = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
        _cancellationCutoffHours = options.Value.CancellationCutoffHours;
    }

    private DateTime ClinicNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);

    private static readonly Expression<Func<Appointment, AppointmentResponse>> ToResponse = a => new AppointmentResponse(
        a.Id,
        a.DoctorId,
        a.Doctor.User.Name,
        a.Doctor.Specialization.Name,
        a.PatientId,
        a.Patient.User.Name,
        a.Slot.Date,
        a.Slot.StartTime,
        a.Slot.EndTime,
        a.Status,
        a.Reason,
        a.CreatedAt);

    public async Task<AppointmentResponse> BookAsync(
        int userId, BookAppointmentRequest request, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException("Patient profile not found.");

        // Tracking ON: RowVersion EF la milto, update chya velela WHERE madhe vaparto
        var slot = await _db.Slots
            .Include(s => s.Doctor).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(s => s.Id == request.SlotId, ct)
            ?? throw new NotFoundException("Slot not found.");

        if (!slot.Doctor.User.IsActive)
            throw new BadRequestException("This doctor is not available.");

        if (slot.IsBooked)
            throw new ConflictException("Slot is already booked.");

        var now = ClinicNow;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        if (slot.Date < today || (slot.Date == today && slot.StartTime <= currentTime))
            throw new BadRequestException("Cannot book a slot in the past.");

        // Patient la same velela dusri booked appointment nako
        var hasClash = await _db.Appointments.AnyAsync(a =>
            a.PatientId == patient.Id &&
            a.Status == AppointmentStatus.Booked &&
            a.Slot.Date == slot.Date &&
            a.Slot.StartTime < slot.EndTime &&
            slot.StartTime < a.Slot.EndTime, ct);

        if (hasClash)
            throw new ConflictException("You already have an appointment at this time.");

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = slot.DoctorId,
            SlotId = slot.Id,
            Status = AppointmentStatus.Booked,
            Reason = request.Reason?.Trim()
        };

        slot.IsBooked = true;
        _db.Appointments.Add(appointment);

        try
        {
            // Slot update + Appointment insert ek ch transaction madhe.
            // UPDATE Slots ... WHERE Id = @id AND RowVersion = @oldVersion
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Dusrya patient ni aadhich book kela
            throw new ConflictException("Slot was just booked by someone else. Please choose another slot.");
        }

        return await _db.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointment.Id)
            .Select(ToResponse)
            .FirstAsync(ct);
    }

  

   

    public async Task<AppointmentResponse> CancelAsync(
    int userId, int appointmentId, CancellationToken ct = default)
    {
        var patientId = await _db.Patients
            .Where(p => p.UserId == userId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Patient profile not found.");

        // PatientId pan match karto, mhanje dusryachi appointment cancel hot nahi (IDOR)
        var appointment = await _db.Appointments
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patientId, ct)
            ?? throw new NotFoundException("Appointment not found.");

        if (appointment.Status != AppointmentStatus.Booked)
            throw new ConflictException($"Appointment is already {appointment.Status}.");

        var slotStart = appointment.Slot.Date.ToDateTime(appointment.Slot.StartTime);

        if (slotStart - ClinicNow < TimeSpan.FromHours(_cancellationCutoffHours))
            throw new BadRequestException(
                $"Appointments can only be cancelled at least {_cancellationCutoffHours} hours before the start time.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.Slot.IsBooked = false;   // slot parat free

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Appointment was modified. Please refresh and try again.");
        }

        return await _db.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(ToResponse)
            .FirstAsync(ct);
    }


    
    public Task<AppointmentResponse> CompleteAsync(int userId, int appointmentId, CancellationToken ct = default)
        => ChangeStatusAsync(userId, appointmentId, AppointmentStatus.Completed, ct);

    public Task<AppointmentResponse> MarkNoShowAsync(int userId, int appointmentId, CancellationToken ct = default)
        => ChangeStatusAsync(userId, appointmentId, AppointmentStatus.NoShow, ct);

   
    private async Task<int> GetDoctorIdAsync(int userId, CancellationToken ct)
        => await _db.Doctors
            .Where(d => d.UserId == userId && d.User.IsActive)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Doctor profile not found.");

    private async Task<AppointmentResponse> ChangeStatusAsync(
        int userId, int appointmentId, AppointmentStatus newStatus, CancellationToken ct)
    {
        var doctorId = await GetDoctorIdAsync(userId, ct);

        // DoctorId pan match karto, mhanje dusrya doctor chi appointment badlat nahi (IDOR)
        var appointment = await _db.Appointments
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.DoctorId == doctorId, ct)
            ?? throw new NotFoundException("Appointment not found.");

        if (appointment.Status != AppointmentStatus.Booked)
            throw new ConflictException($"Appointment is already {appointment.Status}.");

        var slotStart = appointment.Slot.Date.ToDateTime(appointment.Slot.StartTime);

        if (slotStart > ClinicNow)
            throw new BadRequestException("Appointment has not started yet.");

        appointment.Status = newStatus;
        await _db.SaveChangesAsync(ct);

        return await _db.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(ToResponse)
            .FirstAsync(ct);
    }


    public async Task<PagedResponse<AppointmentResponse>> GetMineAsync(
    int userId, AppointmentQuery query, CancellationToken ct = default)
    {
        var patientId = await _db.Patients
            .Where(p => p.UserId == userId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Patient profile not found.");

        // Aadhi ownership (swatacha data), mag filters
        var filtered = ApplyFilters(
            _db.Appointments.AsNoTracking().Where(a => a.PatientId == patientId), query);

        return await ToPageAsync(filtered, newestFirst: true, query, ct);
    }

    public async Task<PagedResponse<AppointmentResponse>> GetForDoctorAsync(
        int userId, AppointmentQuery query, CancellationToken ct = default)
    {
        var doctorId = await GetDoctorIdAsync(userId, ct);

        var filtered = ApplyFilters(
            _db.Appointments.AsNoTracking().Where(a => a.DoctorId == doctorId), query);

        // Doctor la jawal chi appointment aadhi disavi
        return await ToPageAsync(filtered, newestFirst: false, query, ct);
    }

    public async Task<PagedResponse<AppointmentResponse>> GetAllAsync(
        AppointmentQuery query, CancellationToken ct = default)
    {
        var source = _db.Appointments.AsNoTracking().AsQueryable();

        if (query.DoctorId.HasValue)
            source = source.Where(a => a.DoctorId == query.DoctorId.Value);

        return await ToPageAsync(ApplyFilters(source, query), newestFirst: true, query, ct);
    }

    private static IQueryable<Appointment> ApplyFilters(IQueryable<Appointment> source, AppointmentQuery query)
    {
        if (query.Date.HasValue)
            source = source.Where(a => a.Slot.Date == query.Date.Value);

        if (query.Status.HasValue)
            source = source.Where(a => a.Status == query.Status.Value);

        return source;
    }

    private async Task<PagedResponse<AppointmentResponse>> ToPageAsync(
        IQueryable<Appointment> filtered, bool newestFirst, AppointmentQuery query, CancellationToken ct)
    {
        // Total count filter nantar, paging chya aadhi
        var totalCount = await filtered.CountAsync(ct);

        // Id tie-breaker: same vel asli tari page madhe rows repeat/miss hot nahit
        var ordered = newestFirst
            ? filtered.OrderByDescending(a => a.Slot.Date)
                      .ThenByDescending(a => a.Slot.StartTime)
                      .ThenByDescending(a => a.Id)
            : filtered.OrderBy(a => a.Slot.Date)
                      .ThenBy(a => a.Slot.StartTime)
                      .ThenBy(a => a.Id);

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToResponse)
            .ToListAsync(ct);

        return new PagedResponse<AppointmentResponse>(items, query.Page, query.PageSize, totalCount);
    }
}