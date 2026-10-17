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
