using EventTicketing.Infrastructure.Data;
using EventTicketing.Infrastructure.Entities;
using EventTicketing.Infrastructure.Enums;
using EventTicketing.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Infrastructure.Services;

public class EventService : IEventService
{
    private readonly EventTicketingDbContext _context;

    public EventService(EventTicketingDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all events with their pricing tiers, ordered by date.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Event>> GetAllAsync()
        => await _context.Events
            .Include(e => e.PricingTiers)
            .OrderBy(e => e.Date)
            .ToListAsync();

    /// <summary>
    /// Retrieves a specific event by ID, including its pricing tiers and tickets.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <returns>The event with its pricing tiers and tickets, or null if not found.</returns>
    public async Task<Event?> GetByIdAsync(Guid id)
        => await _context.Events
            .Include(e => e.PricingTiers)
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == id);

    /// <summary>
    /// Creates a new event along with its pricing tiers. 
    /// Validates that the total capacity of the tiers matches the event's total capacity.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="tiers"></param>
    /// <returns>The created event with its pricing tiers.</returns>
    /// <exception cref="InvalidEventOperationException"></exception>
    public async Task<Event> CreateAsync(Event entity, List<PricingTier> tiers)
    {
        var totalTierCapacity = tiers.Sum(t => t.Capacity);
        if (totalTierCapacity != entity.TotalCapacity)
            throw new InvalidEventOperationException(
                $"Pricing tier capacities ({totalTierCapacity}) must equal total event capacity ({entity.TotalCapacity}).");

        entity.Id = Guid.NewGuid();
        entity.AvailableTickets = entity.TotalCapacity;
        entity.Status = EventStatus.Draft;
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        foreach (var tier in tiers)
        {
            tier.Id = Guid.NewGuid();
            tier.EventId = entity.Id;
            tier.AvailableTickets = tier.Capacity;
        }

        entity.PricingTiers = tiers;

        _context.Events.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Asynchronously updates an existing event and its pricing tiers. 
    /// </summary>
    /// <param name="id">The unique identifier of the event to update.</param>
    /// <param name="request">The updated event data.</param>
    /// <returns>The updated event.</returns>
    /// <exception cref="EventNotFoundException">Thrown when the event with the specified identifier does not exist.</exception>
    /// <exception cref="InvalidEventOperationException">Thrown when the update operation is not allowed, such as modifying a cancelled event or changing pricing tiers
    /// after ticket sales have started.</exception>
    public async Task<Event> UpdateAsync(Guid id, UpdateEventModel request)
    {
        var existing = await _context.Events
                            .Include(e => e.PricingTiers)
                            .Include(e => e.Tickets)
                            .FirstOrDefaultAsync(e => e.Id == id)
                            ?? throw new EventNotFoundException(id);


        // Prevent updates if event is cancelled
        if (existing.Status == EventStatus.Cancelled)
            throw new InvalidEventOperationException("Cancelled events cannot be modified.");

        // Update basic fields
        if (request.Name != null) existing.Name = request.Name;
        if (request.Description != null) existing.Description = request.Description;
        if (request.Venue != null) existing.Venue = request.Venue;
        if (request.Date != null) existing.Date = request.Date.Date;
        if (request.Time != null) existing.Time = request.Time;

        // Pricing Tier Updates
        if (request.PricingTiers != null)
        {
            var hasSoldTickets = existing.Tickets.Any();

            var existingTiers = existing.PricingTiers.ToList();

            var incomingIds = request.PricingTiers
                .Where(t => t.Id.HasValue)
                .Select(t => t.Id!.Value)
                .ToHashSet();

            // Handle Removed Tiers
            var tiersToRemove = existingTiers
                .Where(t => !incomingIds.Contains(t.Id))
                .ToList();

            if (tiersToRemove.Any())
            {
                if (hasSoldTickets)
                    throw new InvalidEventOperationException("Cannot remove tiers after ticket sales have started.");

                _context.PricingTiers.RemoveRange(tiersToRemove);
            }

            foreach (var tierRequest in request.PricingTiers)
            {
                if (tierRequest.Id.HasValue)
                {
                    // Update Existing Tier
                    var existingTier = existingTiers
                        .FirstOrDefault(t => t.Id == tierRequest.Id.Value)
                        ?? throw new InvalidEventOperationException("Invalid pricing tier ID.");

                    var soldTickets = existingTier.Capacity - existingTier.AvailableTickets;

                    // Prevent invalid capacity reduction
                    if (tierRequest.Capacity < soldTickets)
                        throw new InvalidEventOperationException(
                            $"Cannot reduce capacity below sold tickets ({soldTickets}).");

                    existingTier.Name = tierRequest.Name;
                    existingTier.Price = tierRequest.Price;
                    existingTier.Capacity = tierRequest.Capacity;

                    existingTier.MinQuantityPerOrder = tierRequest.MinQuantityPerOrder;
                    existingTier.MaxQuantityPerOrder = tierRequest.MaxQuantityPerOrder;
                    existingTier.TierType = tierRequest.TierType;
                    existingTier.SaleEndDate = tierRequest.SaleEndDate;

                    existingTier.AvailableTickets = tierRequest.Capacity - soldTickets;
                }
                else
                {
                    // Add New Tier
                    if (hasSoldTickets)
                        throw new InvalidEventOperationException("Cannot add new pricing tiers after ticket sales have started.");

                    var newTier = new PricingTier
                    {
                        Id = Guid.NewGuid(),
                        EventId = existing.Id,
                        Name = tierRequest.Name,
                        Price = tierRequest.Price,
                        Capacity = tierRequest.Capacity,
                        AvailableTickets = tierRequest.Capacity,
                        MinQuantityPerOrder = tierRequest.MinQuantityPerOrder,
                        MaxQuantityPerOrder = tierRequest.MaxQuantityPerOrder,
                        TierType = tierRequest.TierType,
                        SaleEndDate = tierRequest.SaleEndDate
                    };

                    _context.PricingTiers.Add(newTier);
                }
            }

            // Recalculate totals
            existing.TotalCapacity = existing.PricingTiers.Sum(t => t.Capacity);
            existing.AvailableTickets = existing.PricingTiers.Sum(t => t.AvailableTickets);
        }

        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    /// <summary>
    /// Asynchronously updates the status of an event, enforcing valid state transitions and necessary validations before activation.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="newStatus">The new status to set for the event.</param>
    /// <returns>The updated event.</returns>
    /// <exception cref="EventNotFoundException">Thrown when the event with the specified identifier does not exist.</exception>
    /// <exception cref="InvalidEventOperationException"></exception>
    public async Task<Event> UpdateStatusAsync(Guid id, EventStatus newStatus)
    {
        var existing = await GetByIdAsync(id)
            ?? throw new EventNotFoundException(id);

        // Prevent invalid transitions
        switch (existing.Status)
        {
            case EventStatus.Draft:
                if (newStatus != EventStatus.Active && newStatus != EventStatus.Cancelled)
                    throw new InvalidEventOperationException("Draft can only move to Active or Cancelled.");
                break;

            case EventStatus.Active:
                if (newStatus != EventStatus.Completed && newStatus != EventStatus.Cancelled)
                    throw new InvalidEventOperationException("Active can only move to Completed or Cancelled.");
                break;

            case EventStatus.Completed:
            case EventStatus.Cancelled:
                throw new InvalidEventOperationException("Completed or Cancelled events cannot change status.");
        }

        // Additional validation before activation
        if (newStatus == EventStatus.Active)
        {
            if (!existing.PricingTiers.Any())
                throw new InvalidEventOperationException("Cannot activate an event that has no pricing tiers.");

            if (existing.Date <= DateTime.UtcNow)
                throw new InvalidEventOperationException("Cannot activate past events.");
        }

        existing.Status = newStatus;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    /// <summary>
    /// Asynchronously deletes an event if it is in draft status and has no associated tickets.
    /// </summary>
    /// <param name="id">The unique identifier of the event to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="EventNotFoundException">Thrown when the event with the specified identifier does not exist.</exception>
    /// <exception cref="InvalidEventOperationException">Thrown when the event cannot be deleted due to its current status.</exception>
    /// <exception cref="EventDeletionNotAllowedException">Thrown when the event has associated tickets and cannot be deleted.</exception>
    public async Task DeleteAsync(Guid id)
    {
        var existing = await _context.Events
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new EventNotFoundException(id);

        if (existing.Status != EventStatus.Draft)
            throw new InvalidEventOperationException("Only draft events can be deleted.");

        if (existing.Tickets.Any())
            throw new EventDeletionNotAllowedException(id);

        _context.Events.Remove(existing);
        await _context.SaveChangesAsync();
    }
}