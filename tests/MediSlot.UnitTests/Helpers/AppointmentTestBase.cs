using MediSlot.Application.Common;
using MediSlot.Domain.Entities;
using MediSlot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MediSlot.UnitTests.Helpers;

public abstract class AppointmentTestBase : IDisposable
{
    protected readonly TestDatabase TestDb = new();
    protected readonly TestData Data;
    protected readonly AppointmentService Service;

    protected AppointmentTestBase()
    {
        Data = new TestData(TestDb.Context);

        // TimeZone "UTC" ani cutoff 2 tas: tests chi vel kuthlya machine var pan same chalte
        var settings = Options.Create(new ClinicSettings
        {
            TimeZoneId = "UTC",
            CancellationCutoffHours = 2
        });

        Service = new AppointmentService(TestDb.Context, settings, NullLogger<AppointmentService>.Instance);
    }

    // Aaj pasun `days` divas nantar, nishchit vel (udaharan 10:00).
    // Mhanje test kadhihi chalvla tari slot nehmi bhavishyat ch asto
    protected static DateTime InDays(int days, int hour = 10, int minute = 0)
        => DateTime.UtcNow.Date.AddDays(days).AddHours(hour).AddMinutes(minute);

    protected Task<Appointment> GetAppointmentAsync(int id)
        => TestDb.Context.Appointments.AsNoTracking().SingleAsync(a => a.Id == id);

    protected Task<Slot> GetSlotAsync(int id)
        => TestDb.Context.Slots.AsNoTracking().SingleAsync(s => s.Id == id);

    public void Dispose() => TestDb.Dispose();
}