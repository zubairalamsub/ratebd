using RateBd.Engine.Models;

namespace RateBd.Engine.Tests;

internal static class Fixture
{
    public static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    public static readonly Corridor GbpBdt = new("gbp-bdt", "GBP", "BDT", "GB", "BD");
    public static readonly Corridor AedBdt = new("aed-bdt", "AED", "BDT", "AE", "BD");
    public static readonly Corridor UsdBdt = new("usd-bdt", "USD", "BDT", "US", "BD");
    public static readonly Corridor EurBdt = new("eur-bdt", "EUR", "BDT", "IT", "BD");
    public static readonly Corridor MyrBdt = new("myr-bdt", "MYR", "BDT", "MY", "BD");
    public static readonly Corridor SgdBdt = new("sgd-bdt", "SGD", "BDT", "SG", "BD");
    public static readonly Corridor SarBdt = new("sar-bdt", "SAR", "BDT", "SA", "BD");
    public static readonly Corridor KwdBdt = new("kwd-bdt", "KWD", "BDT", "KW", "BD");
    public static readonly Corridor QarBdt = new("qar-bdt", "QAR", "BDT", "QA", "BD");
    public static readonly Corridor OmrBdt = new("omr-bdt", "OMR", "BDT", "OM", "BD");

    public static readonly DateTime FetchedAt = new(2026, 10, 17, 8, 0, 0, DateTimeKind.Utc);

    public static Quote Quote(string provider, decimal amount, decimal receive, DateTime? fetchedAt = null) =>
        new(provider, "gbp-bdt", "GBP", amount, receive / amount, 0m, receive, "Bank deposit", "Within 1 day",
            fetchedAt ?? FetchedAt, Stale: false);
}
