using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Appointments;
using MediSlot.Domain.Entities;
using MediSlot.Domain.Enums;
using MediSlot.UnitTests.Helpers;

namespace MediSlot.UnitTests.Services;

public class AppointmentCancelTests : AppointmentTestBase
{
    private async Task<(Patient Patient, Slot Slot, Appointment Appointment)> BookedAppointmentAsync(
        DateTime start, AppointmentStatus status = AppointmentStatus.Booked, Patient? patient = null)
    {
        var doctor = await Data.AddDoctorAsync();
        patient ??= await Data.AddPatientAsync();
        var slot = await Data.AddSlotAsync(doctor, start);
        var appointment = await Data.AddAppointmentAsync(patient, slot, status);
        return (patient, slot, appointment);
    }

    [Fact]
    public async Task Cancelling_a_far_future_appointment_succeeds_and_frees_the_slot()
    {
        var (patient, slot, appointment) = await BookedAppointmentAsync(InDays(3));

        var response = await Service.CancelAsync(patient.UserId, appointment.Id);

        Assert.Equal(AppointmentStatus.Cancelled, response.Status);

        var saved = await GetAppointmentAsync(appointment.Id);
        Assert.NotNull(saved.CancelledAt);
        Assert.False((await GetSlotAsync(slot.Id)).IsBooked);   // slot parat free
    }

    [Fact]
    public async Task Cancelling_inside_the_cutoff_window_throws_bad_request()
    {
        // 1 tas baki, cutoff 2 tas
        var (patient, slot, appointment) = await BookedAppointmentAsync(DateTime.UtcNow.AddHours(1));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Service.CancelAsync(patient.UserId, appointment.Id));

        Assert.Equal(AppointmentStatus.Booked, (await GetAppointmentAsync(appointment.Id)).Status);
        Assert.True((await GetSlotAsync(slot.Id)).IsBooked);
    }

    [Fact]
    public async Task Cancelling_just_outside_the_cutoff_window_succeeds()
    {
        // 3 tas baki, cutoff 2 tas
        var (patient, _, appointment) = await BookedAppointmentAsync(DateTime.UtcNow.AddHours(3));

        var response = await Service.CancelAsync(patient.UserId, appointment.Id);

        Assert.Equal(AppointmentStatus.Cancelled, response.Status);
    }

    [Fact]
    public async Task Cancelling_an_already_cancelled_appointment_throws_conflict()
    {
        var (patient, _, appointment) = await BookedAppointmentAsync(InDays(3));
        await Service.CancelAsync(patient.UserId, appointment.Id);

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.CancelAsync(patient.UserId, appointment.Id));
    }

    [Fact]
    public async Task Cancelling_a_completed_appointment_throws_conflict()
    {
        var (patient, _, appointment) = await BookedAppointmentAsync(InDays(3), AppointmentStatus.Completed);

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.CancelAsync(patient.UserId, appointment.Id));
    }

    [Fact]
    public async Task Patient_cannot_cancel_someone_elses_appointment()
    {
        var (_, _, appointment) = await BookedAppointmentAsync(InDays(3));
        var stranger = await Data.AddPatientAsync("Stranger");

        // 404 (403 nahi), mhanje ti appointment ahe ki nahi he pan ujagar hot nahi
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.CancelAsync(stranger.UserId, appointment.Id));

        Assert.Equal(AppointmentStatus.Booked, (await GetAppointmentAsync(appointment.Id)).Status);
    }

    [Fact]
    public async Task Cancelling_an_unknown_appointment_throws_not_found()
    {
        var patient = await Data.AddPatientAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.CancelAsync(patient.UserId, 9999));
    }

    [Fact]
    public async Task A_cancelled_slot_can_be_booked_by_another_patient()
    {
        var (patient, slot, appointment) = await BookedAppointmentAsync(InDays(3));
        var other = await Data.AddPatientAsync("Other");

        await Service.CancelAsync(patient.UserId, appointment.Id);
        var response = await Service.BookAsync(other.UserId, new BookAppointmentRequest(slot.Id, null));

        Assert.Equal(AppointmentStatus.Booked, response.Status);
        Assert.True((await GetSlotAsync(slot.Id)).IsBooked);
    }
}