namespace EventTicketing.API.DTOs.Requests;

public class PurchaseTicketRequest
{
    public Guid PricingTierId { get; set; }

    public int Quantity { get; set; }

    public string PurchaserName { get; set; } = string.Empty;

    public string PurchaserEmail { get; set; } = string.Empty;
}