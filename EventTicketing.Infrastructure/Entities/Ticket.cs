using EventTicketing.Infrastructure.Enums;

namespace EventTicketing.Infrastructure.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid PricingTierId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string PurchaserName { get; set; } = string.Empty;
    public string PurchaserEmail { get; set; } = string.Empty;
    public decimal PricePaid { get; set; }

    /// <summary>Current state of this ticket.</summary>
    public TicketStatus Status { get; set; } = TicketStatus.Active;

    public DateTime PurchasedAt { get; set; }

    public Event Event { get; set; } = null!;
    public PricingTier PricingTier { get; set; } = null!;
}