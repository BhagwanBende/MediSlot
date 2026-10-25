using MediSlot.Application.DTOs.Notifications;

namespace MediSlot.Application.Interfaces;

public interface INotificationService
{
    Task SendAppointmentReminderAsync(AppointmentReminder reminder, CancellationToken ct = default);
}