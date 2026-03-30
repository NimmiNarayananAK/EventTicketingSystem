using EventTicketing.Infrastructure.Enums;

namespace EventTicketing.API.DTOs.Requests;

public class CreateEventRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public int TotalCapacity { get; set; }
    public List<PricingTierRequest> PricingTiers { get; set; } = new();
}

public class PricingTierRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int MinQuantityPerOrder { get; set; } = 1;
    public int MaxQuantityPerOrder { get; set; } = 10;

    /// <summary>Defaults to General if not specified.</summary>
    public PricingTierType TierType { get; set; } = PricingTierType.General;

    /// <summary>Required only when TierType is EarlyBird.</summary>
    public DateTime? SaleEndDate { get; set; }
}