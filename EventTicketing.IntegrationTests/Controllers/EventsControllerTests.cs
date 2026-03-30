using EventTicketing.API.DTOs.Requests;
using EventTicketing.API.DTOs.Responses;
using EventTicketing.Infrastructure.Enums;
using EventTicketing.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EventTicketing.IntegrationTests.Controllers;

public class EventsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EventsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CreateEventRequest ValidCreateRequest(int totalCapacity = 100) => new()
    {
        Name = "Integration Test Concert",
        Description = "An integration test event",
        Venue = "Test Arena",
        Date = DateTime.UtcNow.AddDays(30),
        Time = new TimeSpan(19, 0, 0),
        TotalCapacity = totalCapacity,
        PricingTiers = new List<PricingTierRequest>
        {
            new() { Name = "VIP",     Price = 150, Capacity = totalCapacity / 4,
                     TierType = PricingTierType.VIP },
            new() { Name = "General", Price = 60,  Capacity = totalCapacity * 3 / 4,
                     TierType = PricingTierType.General }
        }
    };

    private async Task<EventResponse> CreateEventAsync(int capacity = 100)
    {
        var response = await _client.PostAsJsonAsync("/api/events", ValidCreateRequest(capacity));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    #region Create 

    [Fact]
    public async Task Create_ValidRequest_Returns201WithDraftStatusAndCorrectCapacity()
    {
        var response = await _client.PostAsJsonAsync("/api/events", ValidCreateRequest(100));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        created!.Name.Should().Be("Integration Test Concert");
        created.TotalCapacity.Should().Be(100);
        created.AvailableTickets.Should().Be(100);
        created.Status.Should().Be(EventStatus.Draft.ToString());
        created.PricingTiers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_TierCapacityMismatch_Returns400()
    {
        var request = ValidCreateRequest(100);
        request.PricingTiers = new List<PricingTierRequest>
        {
            new() { Name = "VIP",     Price = 100, Capacity = 30 },
            new() { Name = "General", Price = 50,  Capacity = 40 }  // 70 != 100
        };

        var response = await _client.PostAsJsonAsync("/api/events", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_MissingName_Returns400()
    {
        var request = ValidCreateRequest();
        request.Name = string.Empty;

        var response = await _client.PostAsJsonAsync("/api/events", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_PastDate_Returns400()
    {
        var request = ValidCreateRequest();
        request.Date = DateTime.UtcNow.AddDays(-1);

        var response = await _client.PostAsJsonAsync("/api/events", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    #endregion create

    #region Read 

    [Fact]
    public async Task GetById_ExistingEvent_Returns200()
    {
        var created = await CreateEventAsync();

        var response = await _client.GetAsync($"/api/events/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EventResponse>();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/events/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    #endregion read

    #region Status

    [Fact]
    public async Task UpdateStatus_DraftToActive_Returns200WithActiveStatus()
    {
        var created = await CreateEventAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/events/{created.Id}/status",
            new UpdateEventStatusRequest { Status = EventStatus.Active });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        updated!.Status.Should().Be(EventStatus.Active.ToString());
    }

    #endregion status

    #region Delete

    [Fact]
    public async Task Delete_EventWithNoTickets_Returns204()
    {
        var created = await CreateEventAsync();

        var response = await _client.DeleteAsync($"/api/events/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/events/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    #endregion delete
}