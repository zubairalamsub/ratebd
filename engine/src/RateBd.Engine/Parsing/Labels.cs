namespace RateBd.Engine.Parsing;

/// <summary>Human-readable wording shared by all parsers so the site shows consistent labels.</summary>
internal static class Labels
{
    public const string NotSpecified = "Not specified";
    public const string NotApplicable = "Not applicable";
    public const string BankDeposit = "Bank deposit";
    public const string CashPickup = "Cash pickup";
    public const string MobileWallet = "Mobile wallet";

    /// <summary>Turns a provider's estimated delivery time into "Within N hours/days".</summary>
    public static string Within(TimeSpan duration)
    {
        if (duration <= TimeSpan.FromHours(1))
        {
            return "Within 1 hour";
        }
        if (duration < TimeSpan.FromHours(24))
        {
            return $"Within {Math.Ceiling(duration.TotalHours)} hours";
        }
        var days = (int)Math.Ceiling(duration.TotalDays);
        return days == 1 ? "Within 1 day" : $"Within {days} days";
    }
}
