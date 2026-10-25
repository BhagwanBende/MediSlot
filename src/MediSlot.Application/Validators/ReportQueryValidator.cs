using FluentValidation;
using MediSlot.Application.DTOs.Reports;

namespace MediSlot.Application.Validators;

public class ReportQueryValidator : AbstractValidator<ReportQuery>
{
    private const int MaxRangeDays = 366;

    public ReportQueryValidator()
    {
        RuleFor(x => x.FromDate).NotEmpty().WithMessage("FromDate is required.");
        RuleFor(x => x.ToDate).NotEmpty().WithMessage("ToDate is required.");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must be on or after FromDate.")
            .When(x => x.FromDate != default && x.ToDate != default);

        // Khup motha range = jad query, mhanun 1 varsha cha limit
        RuleFor(x => x)
            .Must(x => x.ToDate.DayNumber - x.FromDate.DayNumber + 1 <= MaxRangeDays)
            .WithName("ToDate")
            .WithMessage($"Report range cannot exceed {MaxRangeDays} days.")
            .When(x => x.FromDate != default && x.ToDate >= x.FromDate);

        RuleFor(x => x.DoctorId).GreaterThan(0).When(x => x.DoctorId.HasValue);
    }
}