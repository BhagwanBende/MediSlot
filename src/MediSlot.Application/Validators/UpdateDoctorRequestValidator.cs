using FluentValidation;
using MediSlot.Application.DTOs.Doctors;

namespace MediSlot.Application.Validators;

public class UpdateDoctorRequestValidator : AbstractValidator<UpdateDoctorRequest>
{
    public UpdateDoctorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SpecializationId).GreaterThan(0);
        RuleFor(x => x.ExperienceYears).InclusiveBetween(0, 60);
        RuleFor(x => x.Fee).GreaterThan(0).LessThanOrEqualTo(100000);
    }
}