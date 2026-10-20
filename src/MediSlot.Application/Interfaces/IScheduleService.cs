using MediSlot.Application.DTOs.Schedules;

namespace MediSlot.Application.Interfaces;

public interface IScheduleService
{
    Task<List<ScheduleResponse>> GetForDoctorAsync(int doctorId, CancellationToken ct = default);
    Task<List<ScheduleResponse>> GetMineAsync(int userId, CancellationToken ct = default);
    Task<ScheduleResponse> AddAsync(int userId, ScheduleRequest request, CancellationToken ct = default);
    Task DeleteAsync(int userId, int scheduleId, CancellationToken ct = default);
}