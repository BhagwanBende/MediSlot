using FluentValidation;
using MediSlot.Application.DTOs.Appointments;

namespace MediSlot.Application.Validators;

public class AppointmentQueryValidator : AbstractValidator<AppointmentQuery>
{
    public AppointmentQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.DoctorId).GreaterThan(0).When(x => x.DoctorId.HasValue);
    }
}