using RateBd.Engine.Models;

namespace RateBd.Engine.Providers;

public interface IRateProvider
{
    string Name { get; }

    Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct);
}

/// <summary>A provider that also exposes the mid-market benchmark details for a corridor.</summary>
public interface IBenchmarkProvider : IRateProvider
{
    MidMarketInfo? GetBenchmark(string corridorId);
}

/// <summary>
/// A provider collected only from some send countries. Corridors from other countries skip it entirely, with no
/// status entry: "not offered" would be wrong (Western Union does serve the UK, we just don't collect it there) or
/// meaningless (a Kuwaiti exchange house on the UK page).
/// </summary>
public interface ISendCountryScoped
{
    /// <summary>ISO 3166-1 alpha-2 send countries, e.g. "KW".</summary>
    IReadOnlySet<string> SendCountries { get; }
}

/// <summary>An exchange house that publishes a rate but no fee or per-amount quote.</summary>
public interface IPostedRateProvider : ISendCountryScoped
{
    string Name { get; }

    Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct);
}
