using FluentValidation;
using MediSlot.Application.DTOs.Specializations;

namespace MediSlot.Application.Validators;

public class SpecializationRequestValidator : AbstractValidator<SpecializationRequest>
{
    public SpecializationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);
    }
}