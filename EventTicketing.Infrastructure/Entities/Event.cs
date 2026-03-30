using EventTicketing.Infrastructure.Enums;

namespace EventTicketing.Infrastructure.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public int TotalCapacity { get; set; }
    public int AvailableTickets { get; set; }

    /// <summary>Current lifecycle state of the event.</summary>
    public EventStatus Status { get; set; } = EventStatus.Draft;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<PricingTier> PricingTiers { get; set; } = new List<PricingTier>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}