using MediSlot.Application.DTOs.Notifications;
using MediSlot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MediSlot.Infrastructure.Services;

// Aaj fakt log. Real email sathi hi ek class replace kara (SMTP/SendGrid), baki code tasa ch
public class LogNotificationService : INotificationService
{
    private readonly ILogger<LogNotificationService> _logger;

    public LogNotificationService(ILogger<LogNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAppointmentReminderAsync(AppointmentReminder reminder, CancellationToken ct = default)
    {
        // Email/phone log karat nahit (personal data), fakt ids ani vel
        _logger.LogInformation(
            "Reminder sent for appointment {AppointmentId} with {DoctorName} on {Date} at {StartTime}",
            reminder.AppointmentId, reminder.DoctorName, reminder.Date, reminder.StartTime);

        return Task.CompletedTask;
    }
}