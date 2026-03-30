namespace EventTicketing.API.DTOs.Responses;

public class EventResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public int TotalCapacity { get; set; }
    public int AvailableTickets { get; set; }
    public string Status { get; set; }
    public List<PricingTierResponse> PricingTiers { get; set; } = new();
}

public class PricingTierResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int AvailableTickets { get; set; }
    public int MinQuantityPerOrder { get; set; }
    public int MaxQuantityPerOrder { get; set; }
    public string TierType { get; set; }
    public DateTime? SaleEndDate { get; set; }
}