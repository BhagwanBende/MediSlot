using FluentValidation;
using MediSlot.Application.DTOs.Doctors;

namespace MediSlot.Application.Validators;

public class DoctorQueryValidator : AbstractValidator<DoctorQuery>
{
    private static readonly string[] AllowedSort = ["name", "fee", "experience"];

    public DoctorQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        // Max limit: konihi pageSize=1000000 pathvun server var load takau naye
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSort.Contains(s!.ToLowerInvariant()))
            .WithMessage("SortBy must be one of: name, fee, experience.")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

        RuleFor(x => x.Search).MaximumLength(100);
    }
}