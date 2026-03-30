namespace EventTicketing.Infrastructure.Exceptions;

public class EarlyBirdExpiredException : Exception
{
    public string TierName { get; }
    public DateTime SaleEndDate { get; }

    public EarlyBirdExpiredException(string tierName, DateTime saleEndDate)
        : base($"The Early Bird tier '{tierName}' sale ended on {saleEndDate:yyyy-MM-dd}.")
    {
        TierName = tierName;
        SaleEndDate = saleEndDate;
    }
}