using EventTicketing.Infrastructure.Entities;

namespace EventTicketing.Infrastructure.Services;

public interface ITicketService
{
    Task<List<Ticket>> PurchaseAsync(Guid eventId, Guid pricingTierId, int quantity, string purchaserName, string purchaserEmail);
    Task<(int TotalAvailable, IEnumerable<PricingTier> Tiers)> GetAvailabilityAsync(Guid eventId);
    Task<List<Ticket>> GetTicketsForEventAsync(Guid eventId);
    Task CancelAsync(Guid eventId, Guid ticketId);
}