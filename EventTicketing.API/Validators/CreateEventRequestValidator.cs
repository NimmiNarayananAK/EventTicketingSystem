using EventTicketing.API.DTOs.Requests;
using EventTicketing.Infrastructure.Enums;
using FluentValidation;

namespace EventTicketing.API.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Event name is required.")
            .MaximumLength(200).WithMessage("Event name must not exceed 200 characters.");

        RuleFor(x => x.Venue)
            .NotEmpty().WithMessage("Venue is required.")
            .MaximumLength(300).WithMessage("Venue must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Event date is required.")
            .GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future.");

        RuleFor(x => x.TotalCapacity)
            .GreaterThan(0).WithMessage("Total capacity must be greater than zero.")
            .LessThanOrEqualTo(100_000).WithMessage("Total capacity cannot exceed 100,000.");

        RuleFor(x => x.PricingTiers)
            .NotEmpty().WithMessage("At least one pricing tier is required.");

        // Tier capacities must exactly sum to total capacity
        RuleFor(x => x)
            .Must(x => x.PricingTiers.Sum(t => t.Capacity) == x.TotalCapacity)
            .WithMessage("The sum of all pricing tier capacities must equal the total event capacity.")
            .When(x => x.PricingTiers.Any());

        RuleForEach(x => x.PricingTiers).SetValidator(new PricingTierRequestValidator());
    }
}

public class PricingTierRequestValidator : AbstractValidator<PricingTierRequest>
{
    public PricingTierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tier name is required.")
            .MaximumLength(100).WithMessage("Tier name must not exceed 100 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.")
            .LessThanOrEqualTo(100_000).WithMessage("Price cannot exceed 100,000.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Tier capacity must be greater than zero.");
        
        RuleFor(x => x.MinQuantityPerOrder)
            .GreaterThan(0).WithMessage("Minimum quantity per order must be greater than zero.");

        RuleFor(x => x.MaxQuantityPerOrder)
            .GreaterThanOrEqualTo(x => x.MinQuantityPerOrder).WithMessage("Maximum quantity per order must be greater than or equal to the minimum quantity per order.");

        // EarlyBird tiers must have a SaleEndDate
        RuleFor(x => x.SaleEndDate)
            .NotNull().WithMessage("Early Bird tiers must have a sale end date.")
            .GreaterThan(DateTime.UtcNow).WithMessage("Early Bird sale end date must be in the future.")
            .When(x => x.TierType == PricingTierType.EarlyBird);

        // Non-EarlyBird tiers should not have a SaleEndDate
        RuleFor(x => x.SaleEndDate)
            .Null().WithMessage("Sale end date is only applicable to Early Bird tiers.")
            .When(x => x.TierType != PricingTierType.EarlyBird);
    }
}