namespace MediSlot.Application.DTOs.Slots;

public record SlotResponse(int Id, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);