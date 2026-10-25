using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Appointments;

namespace MediSlot.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponse> BookAsync(int userId, BookAppointmentRequest request, CancellationToken ct = default);
    Task<PagedResponse<AppointmentResponse>> GetMineAsync(int userId, AppointmentQuery query, CancellationToken ct = default);
    Task<AppointmentResponse> CancelAsync(int userId, int appointmentId, CancellationToken ct = default);
    Task<PagedResponse<AppointmentResponse>> GetForDoctorAsync(int userId, AppointmentQuery query, CancellationToken ct = default);
    Task<AppointmentResponse> CompleteAsync(int userId, int appointmentId, CancellationToken ct = default);
    Task<AppointmentResponse> MarkNoShowAsync(int userId, int appointmentId, CancellationToken ct = default);
    Task<PagedResponse<AppointmentResponse>> GetAllAsync(AppointmentQuery query, CancellationToken ct = default);
}