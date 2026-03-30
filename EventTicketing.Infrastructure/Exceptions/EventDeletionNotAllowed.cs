namespace EventTicketing.Infrastructure.Exceptions;

public class EventDeletionNotAllowedException : Exception
{
    public Guid EventId { get; }

    public EventDeletionNotAllowedException(Guid eventId)
        : base($"Event '{eventId}' cannot be deleted because it has sold tickets. Cancel the event instead.")
    {
        EventId = eventId;
    }
}