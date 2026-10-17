using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateBd.Engine.Models;
using RateBd.Engine.Options;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Benchmark "provider": what you would receive at the mid-market rate with no fee.
/// USD/BDT comes from Bangladesh Bank's published weighted average rate (WAR); GBP and AED are crossed through
/// USD using the free, keyless fawazahmed0/currency-api feed. Bangladesh Bank's site sits behind a JavaScript bot
/// challenge, so CI saves the page with headless Chromium (tools/bb-snapshot) and this provider parses that file.
/// </summary>
public sealed class MidMarketProvider(
    HttpClient http,
    TimeProvider time,
    IOptions<MidMarketOptions> options,
    ILogger<MidMarketProvider> logger) : IBenchmarkProvider
{
    internal const string BangladeshBankUrl = "https://www.bb.org.bd/en/index.php/econdata/exchangerate";
    internal const string BangladeshBankSource = "Bangladesh Bank (USD/BDT weighted average rate)";
    internal const string CrossRateSource = "fawazahmed0/currency-api";

    private readonly MidMarketOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, MidMarketInfo> _benchmarks = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private (BangladeshBankRate Bb, UsdCrossRates Cross, DateTime FetchedAt)? _inputs;

    public string Name => "MidMarket";

    public MidMarketInfo? GetBenchmark(string corridorId) => _benchmarks.GetValueOrDefault(corridorId);

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        if (!c.ReceiveCurrency.Equals("BDT", StringComparison.OrdinalIgnoreCase))
        {
            throw new ProviderNotSupportedException("The mid-market benchmark is built from Bangladesh Bank's USD/BDT rate only");
        }

        var (bb, cross, fetchedAt) = await GetInputsAsync(ct);
        var info = Calculate(c, bb, cross, fetchedAt);
        _benchmarks[c.Id] = info;

        return sendAmounts
            .Select(amount => new Quote(Name, c.Id, c.SendCurrency, amount, info.Rate, 0m,
                Math.Round(amount * info.Rate, 2, MidpointRounding.AwayFromZero),
                Labels.NotApplicable, Labels.NotApplicable, fetchedAt, Stale: false))
            .ToList();
    }

    /// <summary>Mid rate (BDT per send-currency unit) = USD/BDT ÷ (send-currency units per USD).</summary>
    internal static MidMarketInfo Calculate(Corridor c, BangladeshBankRate bb, UsdCrossRates cross, DateTime fetchedAt)
    {
        var sendPerUsd = c.SendCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase) ? 1m : cross.For(c.SendCurrency);
        var rate = Math.Round(bb.UsdBdt / sendPerUsd, 4, MidpointRounding.AwayFromZero);
        return new MidMarketInfo(rate, bb.UsdBdt, bb.EffectiveDate, BangladeshBankSource, sendPerUsd, cross.Date,
            CrossRateSource, fetchedAt, Stale: false);
    }

    private async Task<(BangladeshBankRate, UsdCrossRates, DateTime)> GetInputsAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_inputs is not { } inputs)
            {
                var bb = BangladeshBankParser.Parse(await LoadBangladeshBankHtmlAsync(ct));
                var cross = await LoadCrossRatesAsync(ct);
                inputs = (bb, cross, time.GetUtcNow().UtcDateTime);
                _inputs = inputs;
                logger.LogInformation("Bangladesh Bank USD/BDT {Rate} (effective {Date}); cross rates dated {CrossDate}",
                    bb.UsdBdt, bb.EffectiveDate ?? "unknown", cross.Date ?? "unknown");
            }
            return inputs;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> LoadBangladeshBankHtmlAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_options.BangladeshBankHtmlPath))
        {
            if (!File.Exists(_options.BangladeshBankHtmlPath))
            {
                throw new FileNotFoundException(
                    "Bangladesh Bank snapshot not found; the bb-snapshot step probably failed", _options.BangladeshBankHtmlPath);
            }
            return await File.ReadAllTextAsync(_options.BangladeshBankHtmlPath, ct);
        }

        // Without a snapshot, try the page directly. This normally yields the bot challenge, which the parser reports.
        logger.LogWarning("No Bangladesh Bank snapshot configured; requesting the page directly");
        return await http.GetStringAsync(BangladeshBankUrl, ct);
    }

    private async Task<UsdCrossRates> LoadCrossRatesAsync(CancellationToken ct)
    {
        var errors = new List<string>();
        foreach (var url in _options.CrossRateUrls)
        {
            try
            {
                return CurrencyApiParser.Parse(await http.GetStringAsync(url, ct));
            }
            catch (Exception ex) when (ex is HttpRequestException or FormatException or System.Text.Json.JsonException
                                           or TaskCanceledException && !ct.IsCancellationRequested)
            {
                logger.LogWarning("Cross-rate source {Url} failed: {Message}", url, ex.Message);
                errors.Add($"{url}: {ex.Message}");
            }
        }
        throw new HttpRequestException($"All cross-rate sources failed ({string.Join("; ", errors)})");
    }
}
