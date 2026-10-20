using FluentValidation;
using MediSlot.Application.DTOs.Doctors;

namespace MediSlot.Application.Validators;

public class CreateDoctorRequestValidator : AbstractValidator<CreateDoctorRequest>
{
    public CreateDoctorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(150);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

        RuleFor(x => x.SpecializationId).GreaterThan(0);
        RuleFor(x => x.ExperienceYears).InclusiveBetween(0, 60);
        RuleFor(x => x.Fee).GreaterThan(0).LessThanOrEqualTo(100000);
    }
}