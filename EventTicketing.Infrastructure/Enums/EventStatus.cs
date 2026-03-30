namespace EventTicketing.Infrastructure.Enums;

/// <summary>Represents the lifecycle state of an event.</summary>
public enum EventStatus
{    
    /// <summary>Event is created but not yet open for ticket sales.</summary>
    Draft = 1,

    /// <summary>Event is published and tickets are on sale.</summary>
    Active = 2,

    /// <summary>Event has been cancelled. No new tickets can be sold.</summary>
    Cancelled = 3,

    /// <summary>Event has already taken place.</summary>
    Completed = 4
}