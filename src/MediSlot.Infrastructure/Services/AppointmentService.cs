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

    public AppointmentService(AppDbContext db, IOptions<ClinicSettings> options)
    {
        _db = db;
        _tz = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
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

    public async Task<List<AppointmentResponse>> GetMineAsync(int userId, CancellationToken ct = default)
    {
        var patientId = await _db.Patients
            .Where(p => p.UserId == userId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Patient profile not found.");

        return await _db.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.Slot.Date)
            .ThenByDescending(a => a.Slot.StartTime)
            .Select(ToResponse)
            .ToListAsync(ct);
    }
}