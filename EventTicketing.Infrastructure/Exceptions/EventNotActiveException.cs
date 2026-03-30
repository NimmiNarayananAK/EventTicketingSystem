namespace EventTicketing.Infrastructure.Exceptions;

/// <summary>Raised when an operation requires an Active event but the event is in another state.</summary>
public class EventNotActiveException : Exception
{
    public Guid EventId { get; }

    public EventNotActiveException(Guid eventId)
        : base($"Tickets can only be purchased for Active events. Event '{eventId}' is not Active.")
    {
        EventId = eventId;
    }
}