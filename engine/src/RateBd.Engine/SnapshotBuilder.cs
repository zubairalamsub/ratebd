using RateBd.Engine.Models;

namespace RateBd.Engine;

/// <summary>Outcome of asking one provider for quotes on one corridor.</summary>
public sealed record ProviderResult(string Name, string Status, IReadOnlyList<Quote> Quotes, string? Message)
{
    public static ProviderResult Ok(string name, IReadOnlyList<Quote> quotes) => new(name, ProviderStatus.Ok, quotes, null);

    public static ProviderResult Failed(string name, string message) => new(name, ProviderStatus.Failed, [], message);

    public static ProviderResult NotSupported(string name, string reason) => new(name, ProviderStatus.NotSupported, [], reason);
}

public static class SnapshotBuilder
{
    /// <summary>
    /// Combines this run's results with the previous snapshot. A failed provider's last good quotes are
    /// carried forward with <c>Stale = true</c> (keeping their original FetchedAt); the same applies to the benchmark.
    /// </summary>
    public static CorridorSnapshot Build(
        Corridor corridor,
        IReadOnlyList<ProviderResult> results,
        MidMarketInfo? benchmark,
        CorridorSnapshot? previous,
        DateTime now)
    {
        var statuses = new List<ProviderStatus>();
        var quotes = new List<Quote>();

        foreach (var result in results)
        {
            if (result.Status != ProviderStatus.Failed)
            {
                statuses.Add(new ProviderStatus(result.Name, result.Status, result.Message));
                quotes.AddRange(result.Quotes);
                continue;
            }

            var carried = previous?.Quotes
                .Where(q => q.Provider == result.Name)
                .Select(q => q with { Stale = true })
                .ToList() ?? [];

            if (carried.Count > 0)
            {
                var since = carried.Min(q => q.FetchedAt);
                statuses.Add(new ProviderStatus(result.Name, ProviderStatus.Stale,
                    $"{result.Message} (showing last good quote from {since:yyyy-MM-dd HH:mm} UTC)"));
                quotes.AddRange(carried);
            }
            else
            {
                statuses.Add(new ProviderStatus(result.Name, ProviderStatus.Failed, result.Message));
            }
        }

        var midMarket = benchmark ?? (previous?.MidMarket is { } last ? last with { Stale = true } : null);

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
                .ToList());
    }
}
