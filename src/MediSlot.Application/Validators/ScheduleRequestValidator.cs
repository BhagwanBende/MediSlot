using FluentValidation;
using MediSlot.Application.DTOs.Schedules;

namespace MediSlot.Application.Validators;

public class ScheduleRequestValidator : AbstractValidator<ScheduleRequest>
{
    public ScheduleRequestValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum();

        RuleFor(x => x.SlotDurationMins)
            .InclusiveBetween(5, 120).WithMessage("Slot duration must be between 5 and 120 minutes.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time.");

        // 9:00 te 13:00 (240 min) ani slot 15 min = barobar divide hota. Slot 7 min = nahi
        RuleFor(x => x)
            .Must(x => x.SlotDurationMins > 0
                    && (x.EndTime - x.StartTime).TotalMinutes % x.SlotDurationMins == 0)
            .WithName("SlotDurationMins")
            .WithMessage("Time window must divide evenly into slots.")
            .When(x => x.EndTime > x.StartTime);
    }
}