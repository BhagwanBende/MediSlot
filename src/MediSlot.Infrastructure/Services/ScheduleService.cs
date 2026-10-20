using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Schedules;
using MediSlot.Application.Interfaces;
using MediSlot.Domain.Entities;
using MediSlot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediSlot.Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private readonly AppDbContext _db;

    public ScheduleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ScheduleResponse>> GetForDoctorAsync(int doctorId, CancellationToken ct = default)
    {
        if (!await _db.Doctors.AnyAsync(d => d.Id == doctorId && d.User.IsActive, ct))
            throw new NotFoundException("Doctor not found.");

        return await QuerySchedules(doctorId, ct);
    }

    public async Task<List<ScheduleResponse>> GetMineAsync(int userId, CancellationToken ct = default)
    {
        var doctor = await GetDoctorByUserAsync(userId, ct);
        return await QuerySchedules(doctor.Id, ct);
    }

    public async Task<ScheduleResponse> AddAsync(int userId, ScheduleRequest request, CancellationToken ct = default)
    {
        var doctor = await GetDoctorByUserAsync(userId, ct);

        // Overlap check: same divas, aani doghanchi vel ekmekanvar yete ka
        var overlaps = await _db.DoctorSchedules.AnyAsync(s =>
            s.DoctorId == doctor.Id &&
            s.DayOfWeek == request.DayOfWeek &&
            s.StartTime < request.EndTime &&
            request.StartTime < s.EndTime, ct);

        if (overlaps)
            throw new ConflictException("This time overlaps with an existing schedule for that day.");

        var schedule = new DoctorSchedule
        {
            DoctorId = doctor.Id,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SlotDurationMins = request.SlotDurationMins
        };

        _db.DoctorSchedules.Add(schedule);
        await _db.SaveChangesAsync(ct);

        return ToResponse(schedule);
    }

    public async Task DeleteAsync(int userId, int scheduleId, CancellationToken ct = default)
    {
        var doctor = await GetDoctorByUserAsync(userId, ct);

        // DoctorId pan match karto, mhanje dusrya doctor cha schedule delete hot nahi
        var schedule = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.DoctorId == doctor.Id, ct)
            ?? throw new NotFoundException("Schedule not found.");

        _db.DoctorSchedules.Remove(schedule);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Doctor> GetDoctorByUserAsync(int userId, CancellationToken ct)
        => await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == userId && d.User.IsActive, ct)
           ?? throw new NotFoundException("Doctor profile not found.");

    private async Task<List<ScheduleResponse>> QuerySchedules(int doctorId, CancellationToken ct)
        => await _db.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.DoctorId == doctorId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new ScheduleResponse(
                s.Id, s.DoctorId, s.DayOfWeek, s.StartTime, s.EndTime, s.SlotDurationMins))
            .ToListAsync(ct);

    private static ScheduleResponse ToResponse(DoctorSchedule s)
        => new(s.Id, s.DoctorId, s.DayOfWeek, s.StartTime, s.EndTime, s.SlotDurationMins);
}