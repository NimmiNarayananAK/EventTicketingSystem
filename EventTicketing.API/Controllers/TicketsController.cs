using EventTicketing.API.DTOs.Requests;
using EventTicketing.API.DTOs.Responses;
using EventTicketing.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IValidator<PurchaseTicketRequest> _purchaseValidator;

    public TicketsController(ITicketService ticketService, IValidator<PurchaseTicketRequest> purchaseValidator)
    {
        _ticketService = ticketService;
        _purchaseValidator = purchaseValidator;
    }

    [HttpGet("availability")]
    [ProducesResponseType(typeof(AvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityResponse>> GetAvailability(Guid eventId)
    {
        var (totalAvailable, tiers) = await _ticketService.GetAvailabilityAsync(eventId);

        return Ok(new AvailabilityResponse
        {
            EventId = eventId,
            TotalAvailable = totalAvailable,
            PricingTiers = tiers.Select(p => new PricingTierResponse
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Capacity = p.Capacity,
                AvailableTickets = p.AvailableTickets,
                MaxQuantityPerOrder = p.MaxQuantityPerOrder,
                MinQuantityPerOrder = p.MinQuantityPerOrder,
                TierType = p.TierType.ToString(),
                SaleEndDate = p.SaleEndDate
            }).ToList()
        });
    }

    [HttpPost("purchase")]
    [ProducesResponseType(typeof(PurchaseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseResponse>> Purchase(Guid eventId, [FromBody] PurchaseTicketRequest request)
    {
        var validation = await _purchaseValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var tickets = await _ticketService.PurchaseAsync(eventId, request.PricingTierId, request.Quantity, request.PurchaserName, request.PurchaserEmail);

        return Created(string.Empty, new PurchaseResponse
        {
            Success = true,
            Message = $"Successfully purchased {tickets.Count} ticket(s).",
            Tickets = tickets.Select(t => new TicketResponse
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                PricingTierName = t.PricingTier?.Name ?? string.Empty,
                PricePaid = t.PricePaid,
                PurchaserName = t.PurchaserName,
                PurchaserEmail = t.PurchaserEmail,
                PurchasedAt = t.PurchasedAt,
                Status = t.Status
            }).ToList()
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketResponse>>> GetTickets(Guid eventId)
    {
        var tickets = await _ticketService.GetTicketsForEventAsync(eventId);

        return Ok(tickets.Select(t => new TicketResponse
        {
            Id = t.Id,
            TicketNumber = t.TicketNumber,
            PricingTierName = t.PricingTier.Name,
            PricePaid = t.PricePaid,
            PurchaserName = t.PurchaserName,
            PurchaserEmail = t.PurchaserEmail,
            PurchasedAt = t.PurchasedAt
        }));
    }

    [HttpPost("{ticketId:guid}/cancel")]
    public async Task<IActionResult> CancelTicket(Guid eventId, Guid ticketId)
    {
        await _ticketService.CancelAsync(eventId, ticketId);
        return NoContent();
    }
}