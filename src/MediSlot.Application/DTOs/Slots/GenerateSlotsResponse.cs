namespace MediSlot.Application.DTOs.Slots;

public record GenerateSlotsResponse(DateOnly FromDate, DateOnly ToDate, int Created, int AlreadyExisting);