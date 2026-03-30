using EventTicketing.API.DTOs.Requests;
using EventTicketing.API.DTOs.Responses;
using EventTicketing.Infrastructure.Entities;
using EventTicketing.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IValidator<CreateEventRequest> _validator;

    public EventsController(IEventService eventService, IValidator<CreateEventRequest> validator)
    {
        _eventService = eventService;
        _validator = validator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetAll()
    {
        var events = await _eventService.GetAllAsync();
        return Ok(events.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(Guid id)
    {
        var eventEntity = await _eventService.GetByIdAsync(id);
        return eventEntity is null
            ? NotFound(new { message = $"Event {id} not found." })
            : Ok(MapToResponse(eventEntity));
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> Create([FromBody] CreateEventRequest request)
    {
        var result = await _validator.ValidateAsync(request);

        if (!result.IsValid)
            return BadRequest(result.ToDictionary());

        var entity = new Event
        {
            Name = request.Name,
            Description = request.Description,
            Venue = request.Venue,
            Date = request.Date,
            Time = request.Time,
            TotalCapacity = request.TotalCapacity
        };

        var tiers = request.PricingTiers.Select(t => new PricingTier
        {
            Name = t.Name,
            Price = t.Price,
            Capacity = t.Capacity,
            MinQuantityPerOrder = t.MinQuantityPerOrder,
            MaxQuantityPerOrder = t.MaxQuantityPerOrder,
            TierType = t.TierType,
            SaleEndDate = t.SaleEndDate
        }).ToList();

        var created = await _eventService.CreateAsync(entity, tiers);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToResponse(created));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Update(Guid id, [FromBody] UpdateEventRequest request)
    {        
        var model = new UpdateEventModel
        {
            Name = request.Name,
            Description = request.Description,
            Venue = request.Venue,
            Date = request.Date,
            Time = request.Time,
            PricingTiers = request.PricingTiers?.Select(t => new UpdatePricingTierModel
            {
                Id = t.Id,
                Name = t.Name,
                Price = t.Price,
                Capacity = t.Capacity,
                MinQuantityPerOrder = t.MinQuantityPerOrder,
                MaxQuantityPerOrder = t.MaxQuantityPerOrder,
                TierType = t.TierType,
                SaleEndDate = t.SaleEndDate
            }).ToList()
        };


        var updated = await _eventService.UpdateAsync(id, model);
        return Ok(MapToResponse(updated));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<EventResponse>> UpdateStatus(Guid id, [FromBody] UpdateEventStatusRequest request)
    {
        var updated = await _eventService.UpdateStatusAsync(id, request.Status);
        return Ok(MapToResponse(updated));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteAsync(id);
        return NoContent();
    }

    private static EventResponse MapToResponse(Event e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        Venue = e.Venue,
        Date = e.Date,
        Time = e.Time,
        TotalCapacity = e.TotalCapacity,
        AvailableTickets = e.AvailableTickets,
        Status = e.Status.ToString(),
        PricingTiers = e.PricingTiers.Select(p => new PricingTierResponse
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
    };
}