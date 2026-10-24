using MediSlot.Domain.Enums;

namespace MediSlot.Application.DTOs.Appointments;

public record AppointmentResponse(
    int Id,
    int DoctorId,
    string DoctorName,
    string Specialization,
    int PatientId,
    string PatientName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    AppointmentStatus Status,
    string? Reason,
    DateTime CreatedAt);