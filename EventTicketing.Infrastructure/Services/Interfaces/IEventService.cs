using EventTicketing.Infrastructure.Entities;
using EventTicketing.Infrastructure.Enums;

namespace EventTicketing.Infrastructure.Services;

public interface IEventService
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(Guid id);
    Task<Event> CreateAsync(Event entity, List<PricingTier> tiers);
    Task<Event> UpdateAsync(Guid id, UpdateEventModel request);
    Task<Event> UpdateStatusAsync(Guid id, EventStatus newStatus);
    Task DeleteAsync(Guid id);
}