using EventTicketing.Infrastructure.Enums;

public class UpdateEventModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Venue { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }

    public List<UpdatePricingTierModel>? PricingTiers { get; set; }
}

public class UpdatePricingTierModel
{
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Capacity { get; set; }

    public int MinQuantityPerOrder { get; set; }
    public int MaxQuantityPerOrder { get; set; }

    public PricingTierType TierType { get; set; }
    public DateTime? SaleEndDate { get; set; }
}