using FluentValidation;
using MediSlot.Application.DTOs.Auth;

namespace MediSlot.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(150);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

        RuleFor(x => x.DateOfBirth)
            .Must(d => d < DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Date of birth must be in the past.")
            .Must(d => d > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-120)))
                .WithMessage("Enter a valid date of birth.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .MaximumLength(20);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Enter a valid phone number.");

        RuleFor(x => x.BloodGroup)
            .Matches(@"^(A|B|AB|O)[+-]$").WithMessage("Blood group must be like A+, O-, AB+.")
            .When(x => !string.IsNullOrWhiteSpace(x.BloodGroup));
    }
}