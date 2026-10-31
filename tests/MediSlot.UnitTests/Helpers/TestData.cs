using MediSlot.Domain.Entities;
using MediSlot.Domain.Enums;
using MediSlot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediSlot.UnitTests.Helpers;

// Tests sathi doctor/patient/slot/appointment sopya paddhatine banvto
public class TestData
{
    private readonly AppDbContext _db;

    public TestData(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Doctor> AddDoctorAsync(string name = "Dr. Test", bool active = true)
    {
        var specialization = await _db.Specializations.FirstOrDefaultAsync();

        if (specialization is null)
        {
            specialization = new Specialization { Name = "General Physician" };
            _db.Specializations.Add(specialization);
        }

        var doctor = new Doctor
        {
            User = new User
            {
                Name = name,
                Email = $"{Guid.NewGuid():N}@test.com",
                PasswordHash = "hash",
                Role = UserRole.Doctor,
                IsActive = active
            },
            Specialization = specialization,
            ExperienceYears = 5,
            Fee = 500
        };

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        return doctor;
    }

    public async Task<Patient> AddPatientAsync(string name = "Test Patient")
    {
        var patient = new Patient
        {
            User = new User
            {
                Name = name,
                Email = $"{Guid.NewGuid():N}@test.com",
                PasswordHash = "hash",
                Role = UserRole.Patient
            },
            DateOfBirth = new DateOnly(1996, 5, 10),
            Gender = "Male",
            Phone = "9876543210"
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return patient;
    }

    public async Task<Slot> AddSlotAsync(Doctor doctor, DateTime start, int minutes = 15, bool booked = false)
    {
        var slot = new Slot
        {
            DoctorId = doctor.Id,
            Date = DateOnly.FromDateTime(start),
            StartTime = TimeOnly.FromDateTime(start),
            EndTime = TimeOnly.FromDateTime(start.AddMinutes(minutes)),
            IsBooked = booked,
            RowVersion = new byte[] { 1 }   // SQLite RowVersion generate karat nahi, mhanun test madhe dyaycha
        };

        _db.Slots.Add(slot);
        await _db.SaveChangesAsync();
        return slot;
    }

    public async Task<Appointment> AddAppointmentAsync(
        Patient patient, Slot slot, AppointmentStatus status = AppointmentStatus.Booked)
    {
        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = slot.DoctorId,
            SlotId = slot.Id,
            Status = status,
            Reason = "Test"
        };

        if (status == AppointmentStatus.Booked)
            slot.IsBooked = true;

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return appointment;
    }
}