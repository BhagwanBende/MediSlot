using MediSlot.Application.Common;
using MediSlot.Domain.Entities;
using MediSlot.Domain.Enums;
using MediSlot.UnitTests.Helpers;

namespace MediSlot.UnitTests.Services;

public class AppointmentStatusTests : AppointmentTestBase
{
    private async Task<(Doctor Doctor, Appointment Appointment)> AppointmentAsync(
        DateTime start, AppointmentStatus status = AppointmentStatus.Booked)
    {
        var doctor = await Data.AddDoctorAsync();
        var patient = await Data.AddPatientAsync();
        var slot = await Data.AddSlotAsync(doctor, start);
        var appointment = await Data.AddAppointmentAsync(patient, slot, status);
        return (doctor, appointment);
    }

    [Fact]
    public async Task Doctor_can_complete_an_appointment_after_it_started()
    {
        var (doctor, appointment) = await AppointmentAsync(DateTime.UtcNow.AddHours(-1));

        var response = await Service.CompleteAsync(doctor.UserId, appointment.Id);

        Assert.Equal(AppointmentStatus.Completed, response.Status);
    }

    [Fact]
    public async Task Doctor_cannot_complete_an_appointment_that_has_not_started()
    {
        var (doctor, appointment) = await AppointmentAsync(InDays(3));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Service.CompleteAsync(doctor.UserId, appointment.Id));

        Assert.Equal(AppointmentStatus.Booked, (await GetAppointmentAsync(appointment.Id)).Status);
    }

    [Fact]
    public async Task Doctor_can_mark_no_show_after_it_started()
    {
        var (doctor, appointment) = await AppointmentAsync(DateTime.UtcNow.AddHours(-1));

        var response = await Service.MarkNoShowAsync(doctor.UserId, appointment.Id);

        Assert.Equal(AppointmentStatus.NoShow, response.Status);
    }

    [Fact]
    public async Task A_completed_appointment_cannot_be_completed_again()
    {
        var (doctor, appointment) = await AppointmentAsync(DateTime.UtcNow.AddHours(-1));
        await Service.CompleteAsync(doctor.UserId, appointment.Id);

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.CompleteAsync(doctor.UserId, appointment.Id));
    }

    [Fact]
    public async Task A_cancelled_appointment_cannot_be_completed()
    {
        var (doctor, appointment) = await AppointmentAsync(
            DateTime.UtcNow.AddHours(-1), AppointmentStatus.Cancelled);

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.CompleteAsync(doctor.UserId, appointment.Id));
    }

    [Fact]
    public async Task Doctor_cannot_complete_another_doctors_appointment()
    {
        var (_, appointment) = await AppointmentAsync(DateTime.UtcNow.AddHours(-1));
        var otherDoctor = await Data.AddDoctorAsync("Dr. Other");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.CompleteAsync(otherDoctor.UserId, appointment.Id));

        Assert.Equal(AppointmentStatus.Booked, (await GetAppointmentAsync(appointment.Id)).Status);
    }
}