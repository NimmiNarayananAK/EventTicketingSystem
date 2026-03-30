using EventTicketing.Infrastructure.Data;
using EventTicketing.Infrastructure.Entities;
using EventTicketing.Infrastructure.Enums;
using EventTicketing.Infrastructure.Exceptions;
using EventTicketing.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventTicketing.UnitTests.Services;

public class ReportingServiceTests
{
    private static EventTicketingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventTicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EventTicketingDbContext(options);
    }

    private static async Task<(Guid eventId, Guid tierId)> SeedEventWithTicketsAsync(
        EventTicketingDbContext context,
        int ticketCount = 3,
        decimal price = 50)
    {
        var eventId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        context.Events.Add(new Event
        {
            Id = eventId,
            Name = "Report Event",
            Venue = "Venue",
            Date = DateTime.UtcNow.AddDays(30),
            Time = TimeSpan.FromHours(20),
            TotalCapacity = 100,
            AvailableTickets = 100 - ticketCount,
            Status = EventStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.PricingTiers.Add(new PricingTier
        {
            Id = tierId,
            EventId = eventId,
            Name = "General",
            Price = price,
            Capacity = 100,
            AvailableTickets = 100 - ticketCount,
            TierType = PricingTierType.General
        });

        for (var i = 0; i < ticketCount; i++)
        {
            context.Tickets.Add(new Ticket
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                PricingTierId = tierId,
                TicketNumber = $"TKT-TEST-{i:000}",
                PurchaserName = $"User {i}",
                PurchaserEmail = $"user{i}@test.com",
                PricePaid = price,
                Status = TicketStatus.Active,
                PurchasedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
        return (eventId, tierId);
    }

    [Fact]
    public async Task GetSalesSummaryAsync_ValidEvent_ReturnsCorrectTotalsAndTierBreakdown()
    {
        using var context = CreateContext();
        var sut = new ReportingService(context);
        var (eventId, _) = await SeedEventWithTicketsAsync(context, ticketCount: 3, price: 50);

        var result = await sut.GetSalesSummaryAsync(eventId);

        result.TotalTicketsSold.Should().Be(3);
        result.TotalRevenue.Should().Be(150);
        result.AvailableTickets.Should().Be(97);
        result.TierBreakdown.Should().HaveCount(1);
        result.TierBreakdown.First().TicketsSold.Should().Be(3);
        result.TierBreakdown.First().Revenue.Should().Be(150);
    }

    [Fact]
    public async Task GetSalesSummaryAsync_NonExistentEvent_ThrowsEventNotFoundException()
    {
        using var context = CreateContext();
        var sut = new ReportingService(context);

        var act = async () => await sut.GetSalesSummaryAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<EventNotFoundException>();
    }

    [Fact]
    public async Task GetAllSalesSummariesAsync_MultipleEvents_ReturnsAllSummariesWithCorrectTotals()
    {
        using var context = CreateContext();
        var sut = new ReportingService(context);
        await SeedEventWithTicketsAsync(context, ticketCount: 2, price: 50);
        await SeedEventWithTicketsAsync(context, ticketCount: 5, price: 100);

        var results = (await sut.GetAllSalesSummariesAsync()).ToList();

        results.Should().HaveCount(2);
        results.Sum(r => r.TotalTicketsSold).Should().Be(7);
        results.Sum(r => r.TotalRevenue).Should().Be(600);
    }
}