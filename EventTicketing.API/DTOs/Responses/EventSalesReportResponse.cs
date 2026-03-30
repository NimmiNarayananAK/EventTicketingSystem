namespace EventTicketing.API.DTOs.Responses;

public class SalesSummaryResponse
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int TotalTicketsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int AvailableTickets { get; set; }
    public List<TierSummaryResponse> TierBreakdown { get; set; } = new();
}

public class TierSummaryResponse
{
    public string TierName { get; set; } = string.Empty;
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
    public int Available { get; set; }
}