using MediSlot.Application.DTOs.Appointments;
using MediSlot.Domain.Enums;

namespace MediSlot.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponse> BookAsync(int userId, BookAppointmentRequest request, CancellationToken ct = default);
    Task<List<AppointmentResponse>> GetMineAsync(int userId, CancellationToken ct = default);
    Task<AppointmentResponse> CancelAsync(int userId, int appointmentId, CancellationToken ct = default);
    Task<List<AppointmentResponse>> GetForDoctorAsync(int userId, DateOnly? date, AppointmentStatus? status, CancellationToken ct = default);
    Task<AppointmentResponse> CompleteAsync(int userId, int appointmentId, CancellationToken ct = default);
    Task<AppointmentResponse> MarkNoShowAsync(int userId, int appointmentId, CancellationToken ct = default);
    Task<List<AppointmentResponse>> GetAllAsync(int? doctorId, DateOnly? date, AppointmentStatus? status, CancellationToken ct = default);
}