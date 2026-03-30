using EventTicketing.API.DTOs.Requests;
using EventTicketing.API.DTOs.Responses;
using EventTicketing.Infrastructure.Enums;
using EventTicketing.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EventTicketing.IntegrationTests.Controllers;

public class TicketsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TicketsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>Creates an Active event — required since purchase checks EventStatus.Active.</summary>
    private async Task<EventResponse> CreateActiveEventAsync(int capacity = 50)
    {
        var createRequest = new CreateEventRequest
        {
            Name = "Ticket Test Event",
            Venue = "Test Venue",
            Date = DateTime.UtcNow.AddDays(30),
            Time = new TimeSpan(18, 0, 0),
            TotalCapacity = capacity,
            PricingTiers = new List<PricingTierRequest>
            {
                new() { Name = "General", Price = 50, Capacity = capacity,
                         TierType = PricingTierType.General,
                         MinQuantityPerOrder = 1, MaxQuantityPerOrder = 10 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/events", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var evt = (await createResponse.Content.ReadFromJsonAsync<EventResponse>())!;

        // Must activate — TicketService rejects purchases for non-Active events
        await _client.PatchAsJsonAsync($"/api/events/{evt.Id}/status",
            new UpdateEventStatusRequest { Status = EventStatus.Active });

        return evt;
    }

    private static PurchaseTicketRequest BuildPurchaseRequest(Guid tierId, int quantity = 2) => new()
    {
        PricingTierId = tierId,
        Quantity = quantity,
        PurchaserName = "Jane Doe",
        PurchaserEmail = "jane@test.com"
    };

    #region Availability

    [Fact]
    public async Task GetAvailability_ActiveEvent_Returns200WithCorrectStock()
    {
        var evt = await CreateActiveEventAsync(50);

        var response = await _client.GetAsync($"/api/events/{evt.Id}/tickets/availability");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var availability = await response.Content.ReadFromJsonAsync<AvailabilityResponse>();
        availability!.TotalAvailable.Should().Be(50);
        availability.PricingTiers.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAvailability_NonExistentEvent_Returns404()
    {
        var response = await _client.GetAsync($"/api/events/{Guid.NewGuid()}/tickets/availability");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion Availability

    #region Purchase - happy path

    [Fact]
    public async Task Purchase_ValidRequest_Returns201WithActiveTickets()
    {
        var evt = await CreateActiveEventAsync(50);
        var tierId = evt.PricingTiers.First().Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/events/{evt.Id}/tickets/purchase",
            BuildPurchaseRequest(tierId, 2));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<PurchaseResponse>();
        result!.Success.Should().BeTrue();
        result.Tickets.Should().HaveCount(2);
        result.Tickets.Should().AllSatisfy(t =>
        {
            t.TicketNumber.Should().StartWith("TKT-");
            t.Status.Should().Be(TicketStatus.Active);
        });
    }

    [Fact]
    public async Task Purchase_ValidRequest_DecreasesAvailability()
    {
        var evt = await CreateActiveEventAsync(50);
        var tierId = evt.PricingTiers.First().Id;

        await _client.PostAsJsonAsync(
            $"/api/events/{evt.Id}/tickets/purchase",
            BuildPurchaseRequest(tierId, 3));

        var availResponse = await _client.GetAsync($"/api/events/{evt.Id}/tickets/availability");
        var availability = await availResponse.Content.ReadFromJsonAsync<AvailabilityResponse>();

        availability!.TotalAvailable.Should().Be(47);
        availability.PricingTiers.First().AvailableTickets.Should().Be(47);
    }

    #endregion Purchase - happy path

    #region Overselling prevention

    [Fact]
    public async Task Purchase_MoreThanAvailable_Returns400()
    {
        var evt = await CreateActiveEventAsync(2);
        var tierId = evt.PricingTiers.First().Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/events/{evt.Id}/tickets/purchase",
            BuildPurchaseRequest(tierId, 5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Purchase_ExactlyLastAvailableTickets_Succeeds()
    {
        var evt = await CreateActiveEventAsync(3);
        var tierId = evt.PricingTiers.First().Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/events/{evt.Id}/tickets/purchase",
            BuildPurchaseRequest(tierId, 3));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Purchase_AfterSoldOut_Returns400()
    {
        var evt = await CreateActiveEventAsync(3);
        var tierId = evt.PricingTiers.First().Id;

        await _client.PostAsJsonAsync($"/api/events/{evt.Id}/tickets/purchase",
            BuildPurchaseRequest(tierId, 3));

        var response = await _client.PostAsJsonAsync(
            $"/api/events/{evt.Id}/tickets/purchase",
            BuildPurchaseRequest(tierId, 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion Overselling prevention

    #region Cancellation

    [Fact]
    public async Task Cancel_ActiveTicket_Returns204AndRestoresAvailability()
    {
        var evt = await CreateActiveEventAsync(10);
        var tierId = evt.PricingTiers.First().Id;

        var purchaseResponse = await _client.PostAsJsonAsync(
            $"/api/events/{evt.Id}/tickets/purchase",
            BuildPurchaseRequest(tierId, 2));
        var purchase = (await purchaseResponse.Content.ReadFromJsonAsync<PurchaseResponse>())!;
        var ticketId = purchase.Tickets.First().Id;

        var cancelResponse = await _client.PostAsync(
            $"/api/events/{evt.Id}/tickets/{ticketId}/cancel", null);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var availResponse = await _client.GetAsync($"/api/events/{evt.Id}/tickets/availability");
        var availability = await availResponse.Content.ReadFromJsonAsync<AvailabilityResponse>();
        availability!.TotalAvailable.Should().Be(9);  // 10 - 2 purchased + 1 cancelled
    }

    #endregion Cancellation

    #region Validation

    [Fact]
    public async Task Purchase_InvalidEmail_Returns400()
    {
        var evt = await CreateActiveEventAsync();
        var tierId = evt.PricingTiers.First().Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/events/{evt.Id}/tickets/purchase",
            new PurchaseTicketRequest
            {
                PricingTierId = tierId,
                Quantity = 1,
                PurchaserName = "Test",
                PurchaserEmail = "not-an-email"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Purchase_ZeroQuantity_Returns400()
    {
        var evt = await CreateActiveEventAsync();
        var tierId = evt.PricingTiers.First().Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/events/{evt.Id}/tickets/purchase",
            BuildPurchaseRequest(tierId, 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    #endregion Validation
}