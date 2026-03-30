namespace EventTicketing.Infrastructure.Enums;

/// <summary>
/// Represents the category of a pricing tier for an event.
/// </summary>
public enum PricingTierType
{
    /// <summary>Standard general admission.</summary>
    General = 1,

    /// <summary>Premium access with additional perks.</summary>
    VIP = 2,

    /// <summary>Discounted tier available only until a set sale end date.</summary>
    EarlyBird = 3
}