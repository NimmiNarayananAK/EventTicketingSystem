using EventTicketing.API.DTOs.Requests;
using FluentValidation;

namespace EventTicketing.API.Validators;

public class PurchaseTicketRequestValidator : AbstractValidator<PurchaseTicketRequest>
{
    public PurchaseTicketRequestValidator()
    {
        RuleFor(x => x.PricingTierId)
            .NotEmpty().WithMessage("A valid pricing tier must be selected.");

        // Min/Max are validated at runtime against the tier's own limits in TicketService.
        // This validator guards the basic shape of the request.
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("Quantity cannot exceed 100.");

        RuleFor(x => x.PurchaserName)
            .NotEmpty().WithMessage("Purchaser name is required.")
            .MaximumLength(200).WithMessage("Purchaser name must not exceed 200 characters.");

        RuleFor(x => x.PurchaserEmail)
            .NotEmpty().WithMessage("Purchaser email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");
    }
}