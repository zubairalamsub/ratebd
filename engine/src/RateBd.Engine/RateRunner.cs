using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateBd.Engine.Models;
using RateBd.Engine.Options;
using RateBd.Engine.Providers;
using RateBd.Engine.Storage;

namespace RateBd.Engine;

public sealed class RateRunner(
    IEnumerable<IRateProvider> providers,
    JsonDataStore store,
    IOptions<EngineOptions> options,
    TimeProvider time,
    ILogger<RateRunner> logger)
{
    private readonly EngineOptions _options = options.Value;
    private readonly IReadOnlyList<IRateProvider> _providers = providers.ToList();

    public async Task<IReadOnlyList<CorridorSnapshot>> RunAsync(CancellationToken ct)
    {
        if (_options.Corridors.Count == 0 || _options.SendAmounts.Length == 0)
        {
            throw new InvalidOperationException("Engine:Corridors and Engine:SendAmounts must be configured.");
        }

        logger.LogInformation("Writing to {DataDirectory}; providers: {Providers}",
            store.Root, string.Join(", ", _providers.Select(p => p.Name)));

        var snapshots = new List<CorridorSnapshot>();
        foreach (var corridor in _options.Corridors.Select(c => c.ToCorridor()))
        {
            var results = await Task.WhenAll(_providers.Select(p => FetchAsync(p, corridor, ct)));
            var benchmark = _providers.OfType<IBenchmarkProvider>()
                .Select(p => p.GetBenchmark(corridor.Id))
                .FirstOrDefault(b => b is not null);

            var snapshot = SnapshotBuilder.Build(corridor, results, benchmark, store.ReadLatest(corridor.Id),
                time.GetUtcNow().UtcDateTime);
            store.Write(snapshot);
            snapshots.Add(snapshot);

            foreach (var status in snapshot.Providers)
            {
                logger.LogInformation("{Corridor} {Provider}: {Status}{Message}", corridor.Id, status.Name, status.Status,
                    status.Message is null ? "" : $" - {status.Message}");
            }
        }
        return snapshots;
    }

    private async Task<ProviderResult> FetchAsync(IRateProvider provider, Corridor corridor, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.ProviderTimeoutSeconds));
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var quotes = await provider.GetQuotesAsync(corridor, _options.SendAmounts, timeout.Token);
            if (quotes.Count == 0)
            {
                logger.LogError("{Provider} returned no quotes for {Corridor}", provider.Name, corridor.Id);
                return ProviderResult.Failed(provider.Name, "Provider returned no quotes");
            }
            logger.LogInformation("{Provider} {Corridor}: {Count} quotes in {Elapsed} ms",
                provider.Name, corridor.Id, quotes.Count, stopwatch.ElapsedMilliseconds);
            return ProviderResult.Ok(provider.Name, quotes);
        }
        catch (ProviderNotSupportedException ex)
        {
            logger.LogInformation("{Provider} does not support {Corridor}: {Reason}", provider.Name, corridor.Id, ex.Message);
            return ProviderResult.NotSupported(provider.Name, ex.Message);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogError("{Provider} FAILED on {Corridor}: timed out after {Seconds} s",
                provider.Name, corridor.Id, _options.ProviderTimeoutSeconds);
            return ProviderResult.Failed(provider.Name, $"Timed out after {_options.ProviderTimeoutSeconds} s");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{Provider} FAILED on {Corridor}: {Message}", provider.Name, corridor.Id, ex.Message);
            return ProviderResult.Failed(provider.Name, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>Markdown summary for the GitHub Actions job page.</summary>
    public static string Summarise(IReadOnlyList<CorridorSnapshot> snapshots)
    {
        var sb = new StringBuilder("## ratebd fetch\n\n");
        foreach (var snapshot in snapshots)
        {
            sb.Append($"### {snapshot.Corridor.ToUpperInvariant()}\n\n");
            if (snapshot.MidMarket is { } mid)
            {
                sb.Append($"Mid-market: {mid.Rate} {snapshot.ReceiveCurrency}{(mid.Stale ? " (stale)" : "")}\n\n");
            }
            sb.Append("| Provider | Status | Note |\n|---|---|---|\n");
            foreach (var status in snapshot.Providers)
            {
                var note = status.Message?.Replace("|", "/", StringComparison.Ordinal);
                sb.Append($"| {status.Name} | {status.Status} | {note} |\n");
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
