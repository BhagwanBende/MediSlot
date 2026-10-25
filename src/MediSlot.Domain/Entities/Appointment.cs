using MediSlot.Domain.Enums;

namespace MediSlot.Domain.Entities;

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int SlotId { get; set; }
    public Slot Slot { get; set; } = null!;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    public DateTime? ReminderSentAt { get; set; }
}