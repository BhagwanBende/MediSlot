using MediSlot.Application.DTOs.Appointments;
using MediSlot.Domain.Enums;
using MediSlot.UnitTests.Helpers;

namespace MediSlot.UnitTests.Services;

public class AppointmentQueryTests : AppointmentTestBase
{
    [Fact]
    public async Task Patient_only_sees_their_own_appointments()
    {
        var doctor = await Data.AddDoctorAsync();
        var mine = await Data.AddPatientAsync("Me");
        var other = await Data.AddPatientAsync("Other");

        await Data.AddAppointmentAsync(mine, await Data.AddSlotAsync(doctor, InDays(3)));
        await Data.AddAppointmentAsync(other, await Data.AddSlotAsync(doctor, InDays(4)));

        var result = await Service.GetMineAsync(mine.UserId, new AppointmentQuery());

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Me", result.Items[0].PatientName);
    }

    [Fact]
    public async Task Results_are_paged_and_newest_first()
    {
        var doctor = await Data.AddDoctorAsync();
        var patient = await Data.AddPatientAsync();

        foreach (var days in new[] { 3, 4, 5 })
            await Data.AddAppointmentAsync(patient, await Data.AddSlotAsync(doctor, InDays(days)));

        var page1 = await Service.GetMineAsync(patient.UserId, new AppointmentQuery { Page = 1, PageSize = 2 });
        var page2 = await Service.GetMineAsync(patient.UserId, new AppointmentQuery { Page = 2, PageSize = 2 });

        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.True(page1.HasNext);
        Assert.True(page1.Items[0].Date > page1.Items[1].Date);   // navin pahile

        Assert.Single(page2.Items);
        Assert.True(page2.HasPrevious);
        Assert.False(page2.HasNext);
    }

    [Fact]
    public async Task Status_filter_only_returns_matching_appointments()
    {
        var doctor = await Data.AddDoctorAsync();
        var patient = await Data.AddPatientAsync();

        await Data.AddAppointmentAsync(patient, await Data.AddSlotAsync(doctor, InDays(3)), AppointmentStatus.Booked);
        await Data.AddAppointmentAsync(patient, await Data.AddSlotAsync(doctor, InDays(4)), AppointmentStatus.Cancelled);

        var result = await Service.GetMineAsync(
            patient.UserId, new AppointmentQuery { Status = AppointmentStatus.Booked });

        Assert.Single(result.Items);
        Assert.Equal(AppointmentStatus.Booked, result.Items[0].Status);
    }
}