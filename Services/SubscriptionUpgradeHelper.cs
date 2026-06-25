namespace Backend.Services;

public static class SubscriptionUpgradeHelper
{
    public const int BillingCycleDays = 30;

    public static decimal CalculateUpgradeFee(
        decimal currentPlanPrice,
        decimal newPlanPrice,
        DateTime subscriptionEndDate,
        DateTime? referenceDate = null)
    {
        referenceDate ??= DateTime.Now;
        var now = referenceDate.Value;
        var remainingDays = Math.Max(0, (subscriptionEndDate.Date - now.Date).Days);
        var rawFee = (newPlanPrice - currentPlanPrice) * (remainingDays / (decimal)BillingCycleDays);
        if (rawFee < 0)
            rawFee = 0;

        return Math.Round(rawFee, 0, MidpointRounding.AwayFromZero);
    }
}
