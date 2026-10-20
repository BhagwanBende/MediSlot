namespace MediSlot.Application.DTOs.Schedules;

public record ScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMins);