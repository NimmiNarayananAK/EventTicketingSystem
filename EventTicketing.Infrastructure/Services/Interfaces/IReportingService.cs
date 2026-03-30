namespace EventTicketing.Infrastructure.Services;

public interface IReportingService
{
    Task<EventSalesSummary> GetSalesSummaryAsync(Guid eventId);
    Task<IEnumerable<EventSalesSummary>> GetAllSalesSummariesAsync();
}

public record EventSalesSummary(
    Guid EventId,
    string EventName,
    int TotalTicketsSold,
    decimal TotalRevenue,
    int AvailableTickets,
    IEnumerable<TierSalesSummary> TierBreakdown);

public record TierSalesSummary(
    string TierName,
    int TicketsSold,
    decimal Revenue,
    int Available);