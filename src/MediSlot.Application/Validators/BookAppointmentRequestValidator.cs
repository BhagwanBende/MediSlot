using FluentValidation;
using MediSlot.Application.DTOs.Appointments;

namespace MediSlot.Application.Validators;

public class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.SlotId).GreaterThan(0);

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => x.Reason is not null);
    }
}