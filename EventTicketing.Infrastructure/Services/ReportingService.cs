using EventTicketing.Infrastructure.Data;
using EventTicketing.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Infrastructure.Services;

public class ReportingService : IReportingService
{
    private readonly EventTicketingDbContext _context;

    public ReportingService(EventTicketingDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generates a sales summary for a specific event, including total tickets sold, 
    /// total revenue, available tickets, and a breakdown by pricing tier.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <returns>A sales summary for the specified event.</returns>
    /// <exception cref="EventNotFoundException">Thrown when the specified event is not found.</exception>
    public async Task<EventSalesSummary> GetSalesSummaryAsync(Guid eventId)
    {
        var eventEntity = await _context.Events
            .Include(e => e.PricingTiers)
            .Include(e => e.Tickets.Where(t => t.Status == Enums.TicketStatus.Active))
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new EventNotFoundException(eventId);

        var tierBreakdown = eventEntity.PricingTiers.Select(tier =>
        {
            var tierTickets = eventEntity.Tickets.Where(t => t.PricingTierId == tier.Id).ToList();
            return new TierSalesSummary(
                tier.Name,
                tierTickets.Count,
                tierTickets.Sum(t => t.PricePaid),
                tier.AvailableTickets);
        });

        return new EventSalesSummary(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.Tickets.Count,
            eventEntity.Tickets.Sum(t => t.PricePaid),
            eventEntity.AvailableTickets,
            tierBreakdown);
    }

    /// <summary>
    /// Asynchronously retrieves sales summaries for all events, including ticket and pricing tier breakdowns.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of event sales
    /// summaries.</returns>
    public async Task<IEnumerable<EventSalesSummary>> GetAllSalesSummariesAsync()
    {
        // Single query loads all events with related data — no N+1 queries
        var events = await _context.Events
            .Include(e => e.PricingTiers)
            .Include(e => e.Tickets.Where(t => t.Status == Enums.TicketStatus.Active))
            .OrderBy(e => e.Date)
            .ToListAsync();

        return events.Select(e =>
        {
            var tierBreakdown = e.PricingTiers.Select(tier =>
            {
                var tierTickets = e.Tickets.Where(t => t.PricingTierId == tier.Id).ToList();
                return new TierSalesSummary(
                    tier.Name,
                    tierTickets.Count,
                    tierTickets.Sum(t => t.PricePaid),
                    tier.AvailableTickets);
            });

            return new EventSalesSummary(
                e.Id,
                e.Name,
                e.Tickets.Count,
                e.Tickets.Sum(t => t.PricePaid),
                e.AvailableTickets,
                tierBreakdown);
        });
    }
}