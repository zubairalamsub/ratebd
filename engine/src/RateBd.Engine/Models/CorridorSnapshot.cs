namespace RateBd.Engine.Models;

/// <summary>Everything written to <c>/data/{corridor}.json</c> for one run.</summary>
/// <param name="PostedRateProviders">Status of each exchange house that publishes only a rate.</param>
/// <param name="PostedRates">Rates without a published fee (see <see cref="PostedRate"/>), best rate first.</param>
public sealed record CorridorSnapshot(
    string Corridor,
    string SendCurrency,
    string ReceiveCurrency,
    DateTime GeneratedAt,
    MidMarketInfo? MidMarket,
    IReadOnlyList<ProviderStatus> Providers,
    IReadOnlyList<Quote> Quotes,
    IReadOnlyList<ProviderStatus> PostedRateProviders,
    IReadOnlyList<PostedRate> PostedRates);

/// <summary>Outcome of one provider for one corridor.</summary>
public sealed record ProviderStatus(string Name, string Status, string? Message)
{
    public const string Ok = "ok";
    public const string Failed = "failed";
    public const string Stale = "stale";
    public const string NotSupported = "not_supported";
}

/// <summary>How the mid-market benchmark for a corridor was derived.</summary>
/// <param name="Rate">Receive-currency units per one send-currency unit.</param>
/// <param name="UsdBdt">Bangladesh Bank USD/BDT weighted average rate (WAR).</param>
/// <param name="UsdBdtDate">Effective date printed on the Bangladesh Bank rate sheet.</param>
/// <param name="SendPerUsd">Send-currency units per one USD, from the cross-rate API.</param>
/// <param name="CrossRateDate">Date reported by the cross-rate API.</param>
public sealed record MidMarketInfo(
    decimal Rate,
    decimal UsdBdt,
    string? UsdBdtDate,
    string UsdBdtSource,
    decimal SendPerUsd,
    string? CrossRateDate,
    string CrossRateSource,
    DateTime FetchedAt,
    bool Stale);
