namespace MediSlot.Application.DTOs.Notifications;

public record AppointmentReminder(
    int AppointmentId,
    string PatientName,
    string PatientEmail,
    string DoctorName,
    DateOnly Date,
    TimeOnly StartTime);