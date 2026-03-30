using EventTicketing.Infrastructure.Data;
using EventTicketing.Infrastructure.Entities;
using EventTicketing.Infrastructure.Enums;
using EventTicketing.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace EventTicketing.Infrastructure.Services;

public class TicketService : ITicketService
{
    private readonly EventTicketingDbContext _context;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        EventTicketingDbContext context,
        ILogger<TicketService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Purchases tickets for a specific event and pricing tier, enforcing inventory limits, early bird expiry, 
    /// and tier-specific order quantity constraints. 
    /// This operation is transactional to ensure data integrity in concurrent scenarios.
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="pricingTierId"></param>
    /// <param name="quantity"></param>
    /// <param name="purchaserName">The name of the ticket purchaser.</param>
    /// <param name="purchaserEmail">The email of the ticket purchaser.</param>
    /// <returns>A list of purchased tickets.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the specified pricing tier is not found for the event.</exception>
    /// <exception cref="EarlyBirdExpiredException">Thrown when the early bird sale period has expired for the pricing tier.</exception>
    /// <exception cref="TicketOperationException">Thrown when the ticket purchase violates tier-specific constraints.</exception>
    /// <exception cref="EventNotFoundException">Thrown when the specified event is not found.</exception>
    /// <exception cref="EventNotActiveException">Thrown when the event is not active.</exception>
    public async Task<List<Ticket>> PurchaseAsync(Guid eventId, Guid pricingTierId, int quantity, string purchaserName, string purchaserEmail)
    {
        _logger.LogInformation("Starting ticket purchase for Tier {TierId}", pricingTierId);

        // InMemory provider does not support transactions — use a null transaction for tests
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        IDbContextTransaction? transaction = isInMemory
            ? null
            : await _context.Database.BeginTransactionAsync();

        try
        {
            var tier = await _context.PricingTiers
                .FirstOrDefaultAsync(p => p.Id == pricingTierId && p.EventId == eventId)
                ?? throw new KeyNotFoundException("Pricing tier not found for this event.");

            // Enforce Early Bird expiry
            if (!tier.IsSaleActive(DateTime.UtcNow))
                throw new EarlyBirdExpiredException(tier.Name, tier.SaleEndDate!.Value);

            // Enforce tier-specific order quantity limits
            if (quantity < tier.MinQuantityPerOrder)
                throw new TicketOperationException(
                    $"Minimum order quantity for '{tier.Name}' is {tier.MinQuantityPerOrder}.");

            if (quantity > tier.MaxQuantityPerOrder)
                throw new TicketOperationException(
                    $"Maximum order quantity for '{tier.Name}' is {tier.MaxQuantityPerOrder}.");

            if (tier.AvailableTickets < quantity)
                throw InsufficientTicketsException.ForTier(tier.Name, quantity, tier.AvailableTickets);

            var eventEntity = await _context.Events.FindAsync(eventId)
                ?? throw new EventNotFoundException(eventId);

            if (eventEntity.Status != EventStatus.Active)
                throw new EventNotActiveException(eventId);

            if (eventEntity.AvailableTickets < quantity)
                throw InsufficientTicketsException.ForEvent(
                    eventId, quantity, eventEntity.AvailableTickets);

            var now = DateTime.UtcNow;
            var tickets = Enumerable.Range(0, quantity).Select(_ => new Ticket
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                PricingTierId = pricingTierId,
                PurchaserName = purchaserName,
                PurchaserEmail = purchaserEmail,
                PricePaid = tier.Price,
                PurchasedAt = now,
                Status = TicketStatus.Active,
                TicketNumber = GenerateTicketNumber(eventId)
            }).ToList();

            _context.Tickets.AddRange(tickets);
            tier.AvailableTickets -= quantity;
            eventEntity.AvailableTickets -= quantity;
            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Tickets purchased successfully for {Email}", purchaserEmail);
            return tickets;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();

            _logger.LogError(ex, "Concurrency conflict during ticket purchase");
            throw new TicketOperationException("Another purchase occurred simultaneously. Please retry.");
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();

            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    /// <summary>
    /// Retrieves the total available tickets for an event along with the details of each pricing tier,
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <returns>A tuple containing the total available tickets and the details of each pricing tier.</returns>
    /// <exception cref="EventNotFoundException">Thrown when the specified event is not found.</exception>
    public async Task<(int TotalAvailable, IEnumerable<PricingTier> Tiers)> GetAvailabilityAsync(Guid eventId)
    {
        var eventEntity = await _context.Events
            .Include(e => e.PricingTiers)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Status.Equals(EventStatus.Active))
            ?? throw new EventNotFoundException(eventId);

        return (eventEntity.AvailableTickets, eventEntity.PricingTiers);
    }

    /// <summary>
    /// Retrieves all tickets for a specific event, including their associated pricing tier details.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <returns>A list of tickets for the specified event.</returns>
    public async Task<List<Ticket>> GetTicketsForEventAsync(Guid eventId)
    {
        return await _context.Tickets
            .Include(t => t.PricingTier)
            .Where(t => t.EventId == eventId)// && t.Status == TicketStatus.Active)
            .ToListAsync();
    }

    /// <summary>
    /// Cancels a ticket by updating its status to 'Cancelled'.
    /// Also restores the inventory counts for both the associated pricing tier and the event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="ticketId">The unique identifier of the ticket.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the ticket is not found.</exception>
    /// <exception cref="TicketOperationException">Thrown when the ticket cannot be cancelled.</exception>
    public async Task CancelAsync(Guid eventId, Guid ticketId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.PricingTier)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.EventId == eventId)
            ?? throw new KeyNotFoundException("Ticket not found.");

        if (ticket.Status == TicketStatus.Cancelled)
            throw new TicketOperationException("Ticket already cancelled.");

        // Prevent cancellation after event date
        if (ticket.Event.Date < DateTime.UtcNow)
            throw new TicketOperationException("Cannot cancel tickets after the event date.");
        // Update status
        ticket.Status = TicketStatus.Cancelled;

        // Restore inventory
        ticket.PricingTier.AvailableTickets += 1;
        ticket.Event.AvailableTickets += 1;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Generates a unique ticket number using a combination of the current date, event ID, and a random GUID segment.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <returns>A unique ticket number.</returns>
    private static string GenerateTicketNumber(Guid eventId)
        => $"TKT-{DateTime.UtcNow:yyyyMMdd}-{eventId.ToString("N")[..6].ToUpper()}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}