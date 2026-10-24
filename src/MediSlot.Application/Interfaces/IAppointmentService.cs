using MediSlot.Application.DTOs.Appointments;

namespace MediSlot.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponse> BookAsync(int userId, BookAppointmentRequest request, CancellationToken ct = default);
    Task<List<AppointmentResponse>> GetMineAsync(int userId, CancellationToken ct = default);
}