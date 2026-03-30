using EventTicketing.Infrastructure.Enums;

namespace EventTicketing.API.DTOs.Responses;

public class PurchaseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TicketResponse> Tickets { get; set; } = new();
}

public class TicketResponse
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string PricingTierName { get; set; } = string.Empty;
    public decimal PricePaid { get; set; }
    public string PurchaserName { get; set; } = string.Empty;
    public string PurchaserEmail { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public DateTime PurchasedAt { get; set; }
}

public class AvailabilityResponse
{
    public Guid EventId { get; set; }
    public int TotalAvailable { get; set; }
    public List<PricingTierResponse> PricingTiers { get; set; } = new();
}