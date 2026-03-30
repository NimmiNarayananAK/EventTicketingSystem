using EventTicketing.Infrastructure.Enums;

namespace EventTicketing.Infrastructure.Entities;

public class PricingTier
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int AvailableTickets { get; set; }

    /// <summary>Minimum tickets a purchaser must buy in a single order.</summary>
    public int MinQuantityPerOrder { get; set; } = 1;

    /// <summary>Maximum tickets a purchaser can buy in a single order.</summary>
    public int MaxQuantityPerOrder { get; set; } = 10;

    /// <summary>Classifies the tier (General, VIP, EarlyBird, Student).</summary>
    public PricingTierType TierType { get; set; } = PricingTierType.General;

    /// <summary>
    /// Only relevant for EarlyBird tiers. Tickets cannot be purchased after this date.
    /// Null means no expiry applies.
    /// </summary>
    public DateTime? SaleEndDate { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    /// <summary>Returns true if this tier is currently open for sales.</summary>
    public bool IsSaleActive(DateTime utcNow) =>
        TierType != PricingTierType.EarlyBird || SaleEndDate == null || utcNow <= SaleEndDate;
}