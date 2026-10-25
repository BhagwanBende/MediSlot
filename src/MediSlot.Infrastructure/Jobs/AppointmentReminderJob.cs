using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Notifications;
using MediSlot.Application.Interfaces;
using MediSlot.Domain.Enums;
using MediSlot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediSlot.Infrastructure.Jobs;

public class AppointmentReminderJob
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifier;
    private readonly ClinicSettings _clinic;
    private readonly TimeZoneInfo _tz;
    private readonly ILogger<AppointmentReminderJob> _logger;

    public AppointmentReminderJob(
        AppDbContext db,
        INotificationService notifier,
        IOptions<ClinicSettings> options,
        ILogger<AppointmentReminderJob> logger)
    {
        _db = db;
        _notifier = notifier;
        _clinic = options.Value;
        _tz = TimeZoneInfo.FindSystemTimeZoneById(_clinic.TimeZoneId);
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
        var windowEnd = now.AddHours(_clinic.ReminderLeadHours);

        var fromDate = DateOnly.FromDateTime(now);
        var toDate = DateOnly.FromDateTime(windowEnd);

        // Database kadun fakt date range, ani reminder na gelelya Booked appointments
        var candidates = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.Status == AppointmentStatus.Booked
                     && a.ReminderSentAt == null
                     && a.Slot.Date >= fromDate
                     && a.Slot.Date <= toDate)
            .Select(a => new
            {
                a.Id,
                a.Slot.Date,
                a.Slot.StartTime,
                PatientName = a.Patient.User.Name,
                PatientEmail = a.Patient.User.Email,
                DoctorName = a.Doctor.User.Name
            })
            .ToListAsync();

        // Exact vel check (date + time), clinic timezone madhe
        var due = candidates
            .Where(c =>
            {
                var start = c.Date.ToDateTime(c.StartTime);
                return start > now && start <= windowEnd;
            })
            .ToList();

        int sent = 0, failed = 0;

        foreach (var item in due)
        {
            try
            {
                await _notifier.SendAppointmentReminderAsync(new AppointmentReminder(
                    item.Id, item.PatientName, item.PatientEmail, item.DoctorName, item.Date, item.StartTime));

                // Pathavlyavar mark kara, mhanje parat parat reminder jat nahi
                await _db.Appointments
                    .Where(a => a.Id == item.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.ReminderSentAt, (DateTime?)DateTime.UtcNow));

                sent++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Reminder failed for appointment {AppointmentId}", item.Id);
            }
        }

        _logger.LogInformation("Reminder job finished: {Sent} sent, {Failed} failed", sent, failed);

        // Fail zale tar job fail mark, Hangfire retry karel. Jyanna gele nahit fakt tyanchach reminder parat jatil
        if (failed > 0)
            throw new InvalidOperationException($"{failed} reminder(s) failed. See logs.");
    }
}