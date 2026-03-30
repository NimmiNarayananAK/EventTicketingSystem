namespace EventTicketing.Infrastructure.Exceptions;

public class InsufficientTicketsException : Exception
{
    public int Requested { get; }
    public int Available { get; }

    private InsufficientTicketsException(string message, int requested, int available)
        : base(message)
    {
        Requested = requested;
        Available = available;
    }

    /// <summary>
    /// Raised when the requested quantity exceeds availability within a specific pricing tier.
    /// </summary>
    public static InsufficientTicketsException ForTier(string tierName, int requested, int available)
        => new(
            $"Selected number of seats for '{tierName}' tier exceeds availability.",
            requested,
            available);

    /// <summary>
    /// Raised when the event's overall available ticket count is too low,
    /// regardless of tier (e.g. overall capacity guard).
    /// </summary>
    public static InsufficientTicketsException ForEvent(Guid eventId, int requested, int available)
        => new(
            $"Insufficient tickets for Event: {eventId}. Requested: {requested}, Available: {available}.",
            requested,
            available);
}