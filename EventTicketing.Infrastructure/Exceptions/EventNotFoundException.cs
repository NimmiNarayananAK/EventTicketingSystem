namespace EventTicketing.Infrastructure.Exceptions;

public class EventNotFoundException : Exception
{
    public Guid EventId { get; }

    public EventNotFoundException(Guid eventId)
        : base($"Event with ID '{eventId}' was not found.")
    {
        EventId = eventId;
    }
}