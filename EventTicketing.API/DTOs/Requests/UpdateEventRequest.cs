using EventTicketing.Infrastructure.Enums;
using System.ComponentModel.DataAnnotations;

namespace EventTicketing.API.DTOs.Requests;

public class UpdateEventRequest
{
    //[StringLength(200, MinimumLength = 3)]
    public string? Name { get; set; }

    //[StringLength(2000)]
    public string? Description { get; set; }

    //[StringLength(300)]
    public string? Venue { get; set; }

    public DateTime Date { get; set; }

    public TimeSpan Time { get; set; }
    public int TotalCapacity { get; set; }
    public List<UpdatePricingTierRequest>? PricingTiers { get; set; }
}
public class UpdatePricingTierRequest
{
    public Guid? Id { get; set; } // null = new tier

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Capacity { get; set; }

    public int MinQuantityPerOrder { get; set; } = 1;
    public int MaxQuantityPerOrder { get; set; } = 10;

    public PricingTierType TierType { get; set; }
    public DateTime? SaleEndDate { get; set; }
}