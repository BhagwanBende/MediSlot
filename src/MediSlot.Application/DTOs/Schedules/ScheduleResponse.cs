namespace MediSlot.Application.DTOs.Schedules;

public record ScheduleResponse(
    int Id,
    int DoctorId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMins);