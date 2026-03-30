using EventTicketing.Infrastructure.Data;
using EventTicketing.Infrastructure.Entities;
using EventTicketing.Infrastructure.Enums;
using EventTicketing.Infrastructure.Exceptions;
using EventTicketing.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EventTicketing.UnitTests.Services;

public class TicketServiceTests
{
    private static EventTicketingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventTicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EventTicketingDbContext(options);
    }

    private static async Task<(Guid eventId, Guid tierId)> SeedEventWithTierAsync(
        EventTicketingDbContext context,
        int eventCapacity = 50,
        int tierCapacity = 50,
        int minQty = 1,
        int maxQty = 10,
        PricingTierType tierType = PricingTierType.General,
        DateTime? saleEndDate = null,
        EventStatus status = EventStatus.Active)   // Active by default — required for purchase
    {
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        context.Events.Add(new Event
        {
            Id = eventId,
            Name = "Seed Event",
            Venue = "Seed Venue",
            Date = DateTime.UtcNow.AddDays(30),
            Time = TimeSpan.FromHours(19),
            TotalCapacity = eventCapacity,
            AvailableTickets = eventCapacity,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.PricingTiers.Add(new PricingTier
        {
            Id = tierId,
            EventId = eventId,
            Name = "General",
            Price = 50,
            Capacity = tierCapacity,
            AvailableTickets = tierCapacity,
            MinQuantityPerOrder = minQty,
            MaxQuantityPerOrder = maxQty,
            TierType = tierType,
            SaleEndDate = saleEndDate
        });

        await context.SaveChangesAsync();
        return (eventId, tierId);
    }

    #region Purchase — happy path 

    [Fact]
    public async Task PurchaseAsync_ValidRequest_ReturnsActiveTicketsWithCorrectDetails()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context);

        var tickets = await sut.PurchaseAsync(eventId, tierId, 3, "Alice", "alice@test.com");

        tickets.Should().HaveCount(3);
        tickets.Should().AllSatisfy(t =>
        {
            t.Status.Should().Be(TicketStatus.Active);
            t.PricePaid.Should().Be(50);
            t.TicketNumber.Should().StartWith("TKT-");
        });
    }

    [Fact]
    public async Task PurchaseAsync_ValidRequest_DecreasesTierAndEventAvailability()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context, eventCapacity: 50, tierCapacity: 50);

        await sut.PurchaseAsync(eventId, tierId, 5, "Bob", "bob@test.com");

        var tier = await context.PricingTiers.FindAsync(tierId);
        var evt = await context.Events.FindAsync(eventId);
        tier!.AvailableTickets.Should().Be(45);
        evt!.AvailableTickets.Should().Be(45);
    }

    #endregion  Purchase — happy path

    #region Overselling prevention 

    [Fact]
    public async Task PurchaseAsync_ExceedsTierAvailability_ThrowsInsufficientTicketsException()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context, eventCapacity: 50, tierCapacity: 3);

        var act = async () => await sut.PurchaseAsync(eventId, tierId, 5, "Bob", "bob@test.com");

        await act.Should().ThrowAsync<InsufficientTicketsException>()
            .WithMessage("Selected number of seats for 'General' tier exceeds availability.");
    }

    [Fact]
    public async Task PurchaseAsync_ExceedsEventAvailability_ThrowsInsufficientTicketsException()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);

        // Event has only 2 tickets overall but tier shows 50 — event-level guard fires
        var (eventId, tierId) = await SeedEventWithTierAsync(context, eventCapacity: 2, tierCapacity: 50);

        var act = async () => await sut.PurchaseAsync(eventId, tierId, 5, "Carol", "carol@test.com");

        await act.Should().ThrowAsync<InsufficientTicketsException>()
            .WithMessage("Insufficient tickets for Event: " + eventId +". Requested: 5, Available: 2.");
    }

    [Fact]
    public async Task PurchaseAsync_AfterSoldOut_ThrowsInsufficientTicketsException()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context, eventCapacity: 3, tierCapacity: 3, maxQty: 10);

        await sut.PurchaseAsync(eventId, tierId, 3, "Eve", "eve@test.com");

        var act = async () => await sut.PurchaseAsync(eventId, tierId, 1, "Frank", "frank@test.com");

        await act.Should().ThrowAsync<InsufficientTicketsException>();
    }

    #endregion Overselling prevention

    #region Event status guard 

    [Fact]
    public async Task PurchaseAsync_NonActiveEvent_ThrowsInactiveEventException()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context, status: EventStatus.Draft);

        var act = async () => await sut.PurchaseAsync(eventId, tierId, 1, "User", "u@test.com");

        await act.Should().ThrowAsync<EventNotActiveException>()
            .WithMessage("*Active events*");
    }

    #endregion Event status guard

    #region Quantity limits 

    [Fact]
    public async Task PurchaseAsync_BelowMinQuantity_ThrowsInvalidTicketOperationException()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context, minQty: 2, maxQty: 10);

        var act = async () => await sut.PurchaseAsync(eventId, tierId, 1, "User", "u@test.com");

        await act.Should().ThrowAsync<TicketOperationException>()
            .WithMessage("*Minimum order quantity*2*");
    }

    [Fact]
    public async Task PurchaseAsync_AboveMaxQuantity_ThrowsInvalidTicketOperationException()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context, minQty: 1, maxQty: 4);

        var act = async () => await sut.PurchaseAsync(eventId, tierId, 5, "User", "u@test.com");

        await act.Should().ThrowAsync<TicketOperationException>()
            .WithMessage("*Maximum order quantity*4*");
    }

    #endregion Quantity limits

    #region Early Bird 

    [Fact]
    public async Task PurchaseAsync_ExpiredEarlyBirdTier_ThrowsEarlyBirdExpiredException()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context,
            tierType: PricingTierType.EarlyBird,
            saleEndDate: DateTime.UtcNow.AddDays(-1));

        var act = async () => await sut.PurchaseAsync(eventId, tierId, 1, "User", "u@test.com");

        await act.Should().ThrowAsync<EarlyBirdExpiredException>()
            .WithMessage("*Early Bird*sale ended*");
    }

    #endregion Early Bird

    #region Cancel 

    [Fact]
    public async Task CancelAsync_ActiveTicket_SetsStatusCancelledAndRestoresInventory()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context, eventCapacity: 10, tierCapacity: 10);

        var tickets = await sut.PurchaseAsync(eventId, tierId, 2, "User", "u@test.com");
        var ticketId = tickets.First().Id;

        await sut.CancelAsync(eventId, ticketId);

        var cancelled = await context.Tickets.FindAsync(ticketId);
        cancelled!.Status.Should().Be(TicketStatus.Cancelled);

        // Inventory should be restored by 1
        (await context.PricingTiers.FindAsync(tierId))!.AvailableTickets.Should().Be(9);
        (await context.Events.FindAsync(eventId))!.AvailableTickets.Should().Be(9);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelledTicket_ThrowsInvalidTicketOperationException()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, tierId) = await SeedEventWithTierAsync(context);

        var tickets = await sut.PurchaseAsync(eventId, tierId, 1, "User", "u@test.com");
        await sut.CancelAsync(eventId, tickets.First().Id);

        var act = async () => await sut.CancelAsync(eventId, tickets.First().Id);

        await act.Should().ThrowAsync<TicketOperationException>()
            .WithMessage("*already cancelled*");
    }

    #endregion Cancel

    #region Not found 

    [Fact]
    public async Task PurchaseAsync_TierNotFound_ThrowsKeyNotFoundException()
    {
        using var context = CreateContext();
        var sut = new TicketService(context, NullLogger<TicketService>.Instance);
        var (eventId, _) = await SeedEventWithTierAsync(context);

        var act = async () => await sut.PurchaseAsync(eventId, Guid.NewGuid(), 1, "User", "u@test.com");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Pricing tier not found*");
    }

    #endregion Not found

}