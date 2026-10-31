using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Appointments;
using MediSlot.Domain.Enums;
using MediSlot.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MediSlot.UnitTests.Services;

public class AppointmentBookingTests : AppointmentTestBase
{
    [Fact]
    public async Task Booking_an_available_future_slot_succeeds()
    {
        var doctor = await Data.AddDoctorAsync("Dr. Amit");
        var patient = await Data.AddPatientAsync();
        var slot = await Data.AddSlotAsync(doctor, InDays(3));

        var response = await Service.BookAsync(patient.UserId, new BookAppointmentRequest(slot.Id, "Fever"));

        Assert.Equal(AppointmentStatus.Booked, response.Status);
        Assert.Equal("Dr. Amit", response.DoctorName);
        Assert.Equal("Fever", response.Reason);
        Assert.True((await GetSlotAsync(slot.Id)).IsBooked);
    }

    [Fact]
    public async Task Booking_an_already_booked_slot_throws_conflict()
    {
        var doctor = await Data.AddDoctorAsync();
        var patient = await Data.AddPatientAsync();
        var slot = await Data.AddSlotAsync(doctor, InDays(3), booked: true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.BookAsync(patient.UserId, new BookAppointmentRequest(slot.Id, null)));
    }

    [Fact]
    public async Task Second_patient_cannot_book_a_slot_the_first_patient_took()
    {
        var doctor = await Data.AddDoctorAsync();
        var first = await Data.AddPatientAsync("First");
        var second = await Data.AddPatientAsync("Second");
        var slot = await Data.AddSlotAsync(doctor, InDays(3));

        await Service.BookAsync(first.UserId, new BookAppointmentRequest(slot.Id, null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.BookAsync(second.UserId, new BookAppointmentRequest(slot.Id, null)));

        Assert.Equal(1, await TestDb.Context.Appointments.CountAsync());
    }

    [Fact]
    public async Task Booking_a_slot_in_the_past_throws_bad_request()
    {
        var doctor = await Data.AddDoctorAsync();
        var patient = await Data.AddPatientAsync();
        var slot = await Data.AddSlotAsync(doctor, DateTime.UtcNow.AddHours(-1));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Service.BookAsync(patient.UserId, new BookAppointmentRequest(slot.Id, null)));

        Assert.False((await GetSlotAsync(slot.Id)).IsBooked);
    }

    [Fact]
    public async Task Booking_with_an_inactive_doctor_throws_bad_request()
    {
        var doctor = await Data.AddDoctorAsync(active: false);
        var patient = await Data.AddPatientAsync();
        var slot = await Data.AddSlotAsync(doctor, InDays(3));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            Service.BookAsync(patient.UserId, new BookAppointmentRequest(slot.Id, null)));
    }

    [Fact]
    public async Task Booking_an_unknown_slot_throws_not_found()
    {
        var patient = await Data.AddPatientAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.BookAsync(patient.UserId, new BookAppointmentRequest(9999, null)));
    }

    [Fact]
    public async Task Booking_without_a_patient_profile_throws_not_found()
    {
        var doctor = await Data.AddDoctorAsync();
        var slot = await Data.AddSlotAsync(doctor, InDays(3));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.BookAsync(9999, new BookAppointmentRequest(slot.Id, null)));
    }

    [Fact]
    public async Task Patient_cannot_book_two_overlapping_slots_with_different_doctors()
    {
        var doctorA = await Data.AddDoctorAsync("Dr. A");
        var doctorB = await Data.AddDoctorAsync("Dr. B");
        var patient = await Data.AddPatientAsync();

        var slotA = await Data.AddSlotAsync(doctorA, InDays(3, 10));
        var slotB = await Data.AddSlotAsync(doctorB, InDays(3, 10));   // same vel

        await Service.BookAsync(patient.UserId, new BookAppointmentRequest(slotA.Id, null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.BookAsync(patient.UserId, new BookAppointmentRequest(slotB.Id, null)));
    }

    [Fact]
    public async Task Back_to_back_slots_do_not_clash()
    {
        var doctor = await Data.AddDoctorAsync();
        var patient = await Data.AddPatientAsync();

        var first = await Data.AddSlotAsync(doctor, InDays(3, 10, 0));    // 10:00 te 10:15
        var second = await Data.AddSlotAsync(doctor, InDays(3, 10, 15));  // 10:15 te 10:30

        await Service.BookAsync(patient.UserId, new BookAppointmentRequest(first.Id, null));
        var response = await Service.BookAsync(patient.UserId, new BookAppointmentRequest(second.Id, null));

        Assert.Equal(AppointmentStatus.Booked, response.Status);
        Assert.Equal(2, await TestDb.Context.Appointments.CountAsync());
    }
}