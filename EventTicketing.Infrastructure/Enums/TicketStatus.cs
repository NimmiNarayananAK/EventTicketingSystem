namespace EventTicketing.Infrastructure.Enums
{

    /// <summary>Represents the current state of a purchased ticket.</summary>
    public enum TicketStatus
    {
        /// <summary>Ticket is valid and confirmed.</summary>
        Active = 1,

        /// <summary>Ticket has been used/scanned at the event.</summary>
        Used = 2,

        /// <summary>Ticket has been cancelled and is no longer valid.</summary>
        Cancelled = 3
    }
}
