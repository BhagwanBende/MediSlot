namespace MediSlot.Application.DTOs.Slots;

public record GenerateSlotsRequest(DateOnly FromDate, DateOnly ToDate);