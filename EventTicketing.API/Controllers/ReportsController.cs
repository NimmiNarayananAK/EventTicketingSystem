using EventTicketing.API.DTOs.Responses;
using EventTicketing.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("sales")]
    [ProducesResponseType(typeof(IEnumerable<SalesSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SalesSummaryResponse>>> GetAllSales()
    {
        var summaries = await _reportingService.GetAllSalesSummariesAsync();
        return Ok(summaries.Select(MapToResponse));
    }

    [HttpGet("sales/{eventId:guid}")]
    [ProducesResponseType(typeof(SalesSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SalesSummaryResponse>> GetSalesByEvent(Guid eventId)
    {
        var summary = await _reportingService.GetSalesSummaryAsync(eventId);
        return Ok(MapToResponse(summary));
    }

    private static SalesSummaryResponse MapToResponse(EventSalesSummary s) => new()
    {
        EventId = s.EventId,
        EventName = s.EventName,
        TotalTicketsSold = s.TotalTicketsSold,
        TotalRevenue = s.TotalRevenue,
        AvailableTickets = s.AvailableTickets,
        TierBreakdown = s.TierBreakdown.Select(t => new TierSummaryResponse
        {
            TierName = t.TierName,
            TicketsSold = t.TicketsSold,
            Revenue = t.Revenue,
            Available = t.Available
        }).ToList()
    };
}