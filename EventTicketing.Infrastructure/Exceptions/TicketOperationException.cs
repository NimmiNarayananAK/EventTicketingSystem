namespace EventTicketing.Infrastructure.Exceptions;

/// <summary>
/// Raised when a business rule prevents a ticket operation —
/// e.g. cancelling an already-cancelled ticket, cancelling after event date,
/// or quantity outside allowed bounds.
/// </summary>
public class TicketOperationException : Exception
{
    public TicketOperationException(string message)
        : base(message) { }
}