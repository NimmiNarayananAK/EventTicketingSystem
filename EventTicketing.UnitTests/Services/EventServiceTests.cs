using EventTicketing.Infrastructure.Data;
using EventTicketing.Infrastructure.Entities;
using EventTicketing.Infrastructure.Enums;
using EventTicketing.Infrastructure.Exceptions;
using EventTicketing.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventTicketing.UnitTests.Services;

public class EventServiceTests
{
    private static EventTicketingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventTicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EventTicketingDbContext(options);
    }

    private static (Event entity, List<PricingTier> tiers) BuildValidEvent(int totalCapacity = 100)
    {
        var entity = new Event
        {
            Name = "Test Concert",
            Description = "Unit test event",
            Venue = "Test Arena",
            Date = DateTime.UtcNow.AddDays(30),
            Time = new TimeSpan(19, 0, 0),
            TotalCapacity = totalCapacity
        };

        var tiers = new List<PricingTier>
        {
            new() { Name = "VIP",     Price = 150, Capacity = totalCapacity / 4,
                     MinQuantityPerOrder = 1, MaxQuantityPerOrder = 5,
                     TierType = PricingTierType.VIP },
            new() { Name = "General", Price = 60,  Capacity = totalCapacity * 3 / 4,
                     MinQuantityPerOrder = 1, MaxQuantityPerOrder = 10,
                     TierType = PricingTierType.General }
        };

        return (entity, tiers);
    }

    #region Create 

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsEventWithDraftStatusAndCorrectCapacity()
    {
        using var context = CreateContext();
        var sut = new EventService(context);
        var (entity, tiers) = BuildValidEvent(100);

        var result = await sut.CreateAsync(entity, tiers);

        result.Id.Should().NotBeEmpty();
        result.AvailableTickets.Should().Be(100);
        result.Status.Should().Be(EventStatus.Draft);
        result.PricingTiers.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_TierCapacityMismatch_ThrowsInvalidEventOperationException()
    {
        using var context = CreateContext();
        var sut = new EventService(context);
        var entity = new Event { TotalCapacity = 100 };
        var tiers = new List<PricingTier>
        {
            new() { Capacity = 40 },
            new() { Capacity = 40 }   // sums to 80, not 100
        };

        var act = async () => await sut.CreateAsync(entity, tiers);

        await act.Should().ThrowAsync<InvalidEventOperationException>()
            .WithMessage("*must equal total event capacity*");
    }

    #endregion Create

    #region UpdateStatus

    [Fact]
    public async Task UpdateStatusAsync_DraftToActive_UpdatesStatus()
    {
        using var context = CreateContext();
        var sut = new EventService(context);
        var (entity, tiers) = BuildValidEvent();
        var created = await sut.CreateAsync(entity, tiers);

        var result = await sut.UpdateStatusAsync(created.Id, EventStatus.Active);

        result.Status.Should().Be(EventStatus.Active);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentId_ThrowsEventNotFoundException()
    {
        using var context = CreateContext();
        var sut = new EventService(context);

        var act = async () => await sut.UpdateStatusAsync(Guid.NewGuid(), EventStatus.Active);

        await act.Should().ThrowAsync<EventNotFoundException>();
    }

    #endregion UpdateStatus

    #region Update

    [Fact]
    public async Task UpdateAsync_CancelledEvent_ThrowsInvalidEventOperationException()
    {
        using var context = CreateContext();
        var sut = new EventService(context);
        var (entity, tiers) = BuildValidEvent();
        var created = await sut.CreateAsync(entity, tiers);
        await sut.UpdateStatusAsync(created.Id, EventStatus.Cancelled);

        var act = async () => await sut.UpdateAsync(created.Id, new UpdateEventModel { Name = "New Name" });

        await act.Should().ThrowAsync<InvalidEventOperationException>()
            .WithMessage("*Cancelled events cannot be modified*");
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ThrowsEventNotFoundException()
    {
        using var context = CreateContext();
        var sut = new EventService(context);

        var act = async () => await sut.UpdateAsync(Guid.NewGuid(), new UpdateEventModel { Name = "X" });

        await act.Should().ThrowAsync<EventNotFoundException>();
    }

    #endregion Update

    #region Delete

    [Fact]
    public async Task DeleteAsync_EventWithNoTickets_DeletesSuccessfully()
    {
        using var context = CreateContext();
        var sut = new EventService(context);
        var (entity, tiers) = BuildValidEvent();
        var created = await sut.CreateAsync(entity, tiers);

        await sut.DeleteAsync(created.Id);

        context.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_EventWithSoldTickets_ThrowsEventDeletionNotAllowedException()
    {
        using var context = CreateContext();
        var sut = new EventService(context);
        var (entity, tiers) = BuildValidEvent();
        var created = await sut.CreateAsync(entity, tiers);

        context.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            EventId = created.Id,
            PricingTierId = created.PricingTiers.First().Id,
            TicketNumber = "TKT-TEST-001",
            PurchaserName = "Test User",
            PurchaserEmail = "test@test.com",
            PricePaid = 60,
            Status = TicketStatus.Active,
            PurchasedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var act = async () => await sut.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<EventDeletionNotAllowedException>();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ThrowsEventNotFoundException()
    {
        using var context = CreateContext();
        var sut = new EventService(context);

        var act = async () => await sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<EventNotFoundException>();
    }
    #endregion Delete
}