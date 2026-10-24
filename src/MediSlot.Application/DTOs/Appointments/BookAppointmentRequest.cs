namespace MediSlot.Application.DTOs.Appointments;

public record BookAppointmentRequest(int SlotId, string? Reason);