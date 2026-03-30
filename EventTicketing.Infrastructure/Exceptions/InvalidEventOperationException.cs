namespace EventTicketing.Infrastructure.Exceptions;

/// <summary>
/// Raised when a business rule prevents an event management operation —
/// e.g. modifying a cancelled event, invalid status transition,
/// tier changes after ticket sales, or deleting a non-draft event.
/// </summary>
public class InvalidEventOperationException : Exception
{
    public InvalidEventOperationException(string message)
        : base(message) { }
}