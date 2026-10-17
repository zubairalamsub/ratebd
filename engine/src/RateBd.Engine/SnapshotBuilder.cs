using RateBd.Engine.Models;

namespace RateBd.Engine;

/// <summary>Outcome of asking one provider for quotes on one corridor.</summary>
public sealed record ProviderResult(string Name, string Status, IReadOnlyList<Quote> Quotes, string? Message)
{
    public static ProviderResult Ok(string name, IReadOnlyList<Quote> quotes) => new(name, ProviderStatus.Ok, quotes, null);

    public static ProviderResult Failed(string name, string message) => new(name, ProviderStatus.Failed, [], message);

    public static ProviderResult NotSupported(string name, string reason) => new(name, ProviderStatus.NotSupported, [], reason);
}

/// <summary>Outcome of asking one exchange house for its posted rates on one corridor.</summary>
public sealed record PostedRateResult(string Name, string Status, IReadOnlyList<PostedRate> Rates, string? Message)
{
    public static PostedRateResult Ok(string name, IReadOnlyList<PostedRate> rates) => new(name, ProviderStatus.Ok, rates, null);

    public static PostedRateResult Failed(string name, string message) => new(name, ProviderStatus.Failed, [], message);

    public static PostedRateResult NotSupported(string name, string reason) => new(name, ProviderStatus.NotSupported, [], reason);
}

public static class SnapshotBuilder
{
    /// <summary>
    /// A posted rate further than this from the mid-market benchmark is taken to be a parsing or source error (a
    /// renumbered region, a cash-note column) and dropped. Real rates seen sit within about 3% either side.
    /// </summary>
    public const decimal MaxPostedRateDeviation = 0.10m;

    /// <summary>
    /// Combines this run's results with the previous snapshot. A failed provider's last good quotes (or posted rates)
    /// are carried forward with <c>Stale = true</c>, keeping their original FetchedAt; the same applies to the benchmark.
    /// </summary>
    public static CorridorSnapshot Build(
        Corridor corridor,
        IReadOnlyList<ProviderResult> results,
        MidMarketInfo? benchmark,
        CorridorSnapshot? previous,
        DateTime now,
        IReadOnlyList<PostedRateResult>? postedResults = null)
    {
        var midMarket = benchmark ?? (previous?.MidMarket is { } last ? last with { Stale = true } : null);

        var statuses = new List<ProviderStatus>();
        var quotes = new List<Quote>();
        foreach (var result in results)
        {
            var (status, items) = CarryForward(result.Name, result.Status, result.Message, result.Quotes,
                previous?.Quotes.Where(q => q.Provider == result.Name).Select(q => q with { Stale = true }),
                q => q.FetchedAt, "quote");
            statuses.Add(status);
            quotes.AddRange(items);
        }

        var postedStatuses = new List<ProviderStatus>();
        var posted = new List<PostedRate>();
        foreach (var raw in postedResults ?? [])
        {
            var result = DropImplausible(raw, midMarket);
            var (status, items) = CarryForward(result.Name, result.Status, result.Message, result.Rates,
                previous?.PostedRates.Where(r => r.Provider == result.Name).Select(r => r with { Stale = true }),
                r => r.FetchedAt, "rate");
            postedStatuses.Add(status);
            posted.AddRange(items);
        }

        return new CorridorSnapshot(
            corridor.Id,
            corridor.SendCurrency,
            corridor.ReceiveCurrency,
            now,
            midMarket,
            statuses,
            quotes
                .OrderBy(q => q.SendAmount)
                .ThenByDescending(q => q.ReceiveAmount)
                .ToList(),
            postedStatuses,
            posted
                .OrderByDescending(r => r.Rate)
                .ThenBy(r => r.Provider, StringComparer.Ordinal)
                .ToList());
    }

    /// <summary>Removes posted rates that are implausibly far from the benchmark; none left makes the result a failure.</summary>
    internal static PostedRateResult DropImplausible(PostedRateResult result, MidMarketInfo? midMarket)
    {
        if (result.Status != ProviderStatus.Ok || midMarket is not { Rate: > 0 } mid)
        {
            return result;
        }
        var implausible = result.Rates.Where(r => Math.Abs(r.Rate / mid.Rate - 1) > MaxPostedRateDeviation).ToList();
        if (implausible.Count == 0)
        {
            return result;
        }
        var reason = $"rate {implausible[0].Rate} is more than {MaxPostedRateDeviation:P0} from the mid-market rate {mid.Rate}";
        var kept = result.Rates.Except(implausible).ToList();
        return kept.Count == 0
            ? PostedRateResult.Failed(result.Name, $"Implausible: {reason}")
            : result with { Rates = kept, Message = $"Dropped {implausible.Count} implausible rate(s): {reason}" };
    }

    private static (ProviderStatus Status, IReadOnlyList<T> Items) CarryForward<T>(
        string name,
        string status,
        string? message,
        IReadOnlyList<T> items,
        IEnumerable<T>? previousItems,
        Func<T, DateTime> fetchedAt,
        string noun)
    {
        if (status != ProviderStatus.Failed)
        {
            return (new ProviderStatus(name, status, message), items);
        }

        var carried = previousItems?.ToList() ?? [];
        if (carried.Count == 0)
        {
            return (new ProviderStatus(name, ProviderStatus.Failed, message), []);
        }

        var since = carried.Min(fetchedAt);
        return (new ProviderStatus(name, ProviderStatus.Stale,
            $"{message} (showing last good {noun} from {since:yyyy-MM-dd HH:mm} UTC)"), carried);
    }
}
