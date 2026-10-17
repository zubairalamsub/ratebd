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
    IEnumerable<IPostedRateProvider> postedRateProviders,
    JsonDataStore store,
    IOptions<EngineOptions> options,
    TimeProvider time,
    ILogger<RateRunner> logger)
{
    private readonly EngineOptions _options = options.Value;
    private readonly IReadOnlyList<IRateProvider> _providers = providers.ToList();
    private readonly IReadOnlyList<IPostedRateProvider> _postedRateProviders = postedRateProviders.ToList();

    public async Task<IReadOnlyList<CorridorSnapshot>> RunAsync(CancellationToken ct)
    {
        if (_options.Corridors.Count == 0)
        {
            throw new InvalidOperationException("Engine:Corridors must be configured.");
        }
        var missingAmounts = _options.Corridors.Where(c => c.SendAmounts.Length == 0 && _options.SendAmounts.Length == 0).ToList();
        if (missingAmounts.Count > 0)
        {
            throw new InvalidOperationException(
                $"No send amounts for {string.Join(", ", missingAmounts.Select(c => c.Id))}: set Engine:SendAmounts or the corridor's SendAmounts.");
        }

        logger.LogInformation("Writing to {DataDirectory}; providers: {Providers}; posted rates: {PostedRateProviders}",
            store.Root, string.Join(", ", _providers.Select(p => p.Name)),
            string.Join(", ", _postedRateProviders.Select(p => p.Name)));

        var snapshots = new List<CorridorSnapshot>();
        foreach (var options in _options.Corridors)
        {
            var corridor = options.ToCorridor();
            var amounts = options.SendAmounts.Length > 0 ? options.SendAmounts : _options.SendAmounts;
            var quoting = _providers.Where(p => Serves(p, corridor)).ToList();
            var posting = _postedRateProviders.Where(p => Serves(p, corridor)).ToList();
            var resultsTask = Task.WhenAll(quoting.Select(p => FetchQuotesAsync(p, corridor, amounts, ct)));
            var postedTask = Task.WhenAll(posting.Select(p => FetchPostedRatesAsync(p, corridor, ct)));
            var results = await resultsTask;
            var posted = await postedTask;
            var benchmark = quoting.OfType<IBenchmarkProvider>()
                .Select(p => p.GetBenchmark(corridor.Id))
                .FirstOrDefault(b => b is not null);

            var snapshot = SnapshotBuilder.Build(corridor, results, benchmark, store.ReadLatest(corridor.Id),
                time.GetUtcNow().UtcDateTime, posted);
            store.Write(snapshot);
            snapshots.Add(snapshot);

            foreach (var status in snapshot.Providers.Concat(snapshot.PostedRateProviders))
            {
                logger.LogInformation("{Corridor} {Provider}: {Status}{Message}", corridor.Id, status.Name, status.Status,
                    status.Message is null ? "" : $" - {status.Message}");
            }
        }
        return snapshots;
    }

    private static bool Serves(object provider, Corridor corridor) =>
        provider is not ISendCountryScoped scoped || scoped.SendCountries.Contains(corridor.SendCountry);

    private async Task<ProviderResult> FetchQuotesAsync(IRateProvider provider, Corridor corridor, decimal[] amounts,
        CancellationToken ct)
    {
        var (status, quotes, message) = await FetchAsync(provider.Name, corridor, "quotes",
            token => provider.GetQuotesAsync(corridor, amounts, token), ct);
        return new ProviderResult(provider.Name, status, quotes, message);
    }

    private async Task<PostedRateResult> FetchPostedRatesAsync(IPostedRateProvider provider, Corridor corridor,
        CancellationToken ct)
    {
        var (status, rates, message) = await FetchAsync(provider.Name, corridor, "posted rates",
            token => provider.GetRatesAsync(corridor, token), ct);
        return new PostedRateResult(provider.Name, status, rates, message);
    }

    /// <summary>Runs one provider call under the per-provider timeout and turns every outcome into a status.</summary>
    private async Task<(string Status, IReadOnlyList<T> Items, string? Message)> FetchAsync<T>(
        string name,
        Corridor corridor,
        string what,
        Func<CancellationToken, Task<IReadOnlyList<T>>> fetch,
        CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.ProviderTimeoutSeconds));
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var items = await fetch(timeout.Token);
            if (items.Count == 0)
            {
                logger.LogError("{Provider} returned no {What} for {Corridor}", name, what, corridor.Id);
                return (ProviderStatus.Failed, [], $"Provider returned no {what}");
            }
            logger.LogInformation("{Provider} {Corridor}: {Count} {What} in {Elapsed} ms",
                name, corridor.Id, items.Count, what, stopwatch.ElapsedMilliseconds);
            return (ProviderStatus.Ok, items, null);
        }
        catch (ProviderNotSupportedException ex)
        {
            logger.LogInformation("{Provider} does not support {Corridor}: {Reason}", name, corridor.Id, ex.Message);
            return (ProviderStatus.NotSupported, [], ex.Message);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogError("{Provider} FAILED on {Corridor}: timed out after {Seconds} s",
                name, corridor.Id, _options.ProviderTimeoutSeconds);
            return (ProviderStatus.Failed, [], $"Timed out after {_options.ProviderTimeoutSeconds} s");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{Provider} FAILED on {Corridor}: {Message}", name, corridor.Id, ex.Message);
            return (ProviderStatus.Failed, [], $"{ex.GetType().Name}: {ex.Message}");
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
            var all = snapshot.Providers.Select(p => (Status: p, Posted: false))
                .Concat(snapshot.PostedRateProviders.Select(p => (Status: p, Posted: true)));
            foreach (var (status, posted) in all)
            {
                var note = status.Message?.Replace("|", "/", StringComparison.Ordinal);
                sb.Append($"| {status.Name}{(posted ? " (posted rate)" : "")} | {status.Status} | {note} |\n");
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
