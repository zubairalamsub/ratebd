using System.Net;
using Microsoft.Extensions.Logging;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Remitly's public calculator endpoint, called the way remitly.com's own calculator calls it: on
/// <c>api.remitly.com</c>, with the server-issued <c>Remitly-DeviceEnvironmentID</c> and the
/// <c>remittance-distro-id</c> header. The device ID is embedded in every public converter page (and set as the
/// <c>de_id</c> cookie), so it is fetched once per run with a plain GET.
/// <para>
/// Anonymous calls without the device ID were answered with HTTP 429 <c>NOT_ALLOWED</c> far more often in testing
/// (2026-10-17: 0 of 15 requests with the header vs 7 of 20 without). On a 429 that survives Polly's retries the
/// provider fetches a fresh device ID and tries once more before failing.
/// </para>
/// </summary>
public sealed class RemitlyProvider(HttpClient http, TimeProvider time, ILogger<RemitlyProvider> logger) : IRateProvider
{
    internal const string EstimateUrl = "https://api.remitly.com/v3/calculator/estimate";
    internal const string SiteOrigin = "https://www.remitly.com";

    internal static readonly TimeSpan PauseBetweenRequests = TimeSpan.FromSeconds(3);
    internal static readonly TimeSpan PauseBeforeFreshDeviceRetry = TimeSpan.FromSeconds(10);

    // Remitly conduits use ISO 3166-1 alpha-3 country codes.
    private static readonly Dictionary<string, string> Alpha3 = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GB"] = "GBR",
        ["AE"] = "ARE",
        ["BD"] = "BGD",
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _deviceId;

    public string Name => "Remitly";

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        if (!Alpha3.TryGetValue(c.SendCountry, out var from) || !Alpha3.TryGetValue(c.ReceiveCountry, out var to))
        {
            throw new ProviderNotSupportedException($"No Remitly conduit mapping for {c.SendCountry}→{c.ReceiveCountry}");
        }

        var conduit = Uri.EscapeDataString($"{from}:{c.SendCurrency}-{to}:{c.ReceiveCurrency}");
        var quotes = new List<Quote>();
        foreach (var amount in sendAmounts)
        {
            if (quotes.Count > 0)
            {
                await Task.Delay(PauseBetweenRequests, time, ct);
            }
            var url = $"{EstimateUrl}?conduit={conduit}&anchor=SEND&amount={amount:0.##}&purpose=OTHER" +
                      "&customer_segment=STANDARD&customer_recognition=UNRECOGNIZED&strict_promo=false";
            var json = await GetEstimateAsync(c, url, ct);
            quotes.Add(RemitlyParser.Parse(json, c, amount, time.GetUtcNow().UtcDateTime));
        }
        return quotes;
    }

    private async Task<string> GetEstimateAsync(Corridor c, string url, CancellationToken ct)
    {
        var deviceId = await GetDeviceIdAsync(c, refresh: false, ct);
        using (var response = await SendEstimateAsync(url, deviceId, ct))
        {
            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(ct);
            }
        }

        logger.LogWarning("Remitly still answered 429 after retries; retrying once with a fresh device ID");
        await Task.Delay(PauseBeforeFreshDeviceRetry, time, ct);
        deviceId = await GetDeviceIdAsync(c, refresh: true, ct);
        using var retry = await SendEstimateAsync(url, deviceId, ct);
        retry.EnsureSuccessStatusCode();
        return await retry.Content.ReadAsStringAsync(ct);
    }

    private async Task<HttpResponseMessage> SendEstimateAsync(string url, string? deviceId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Referrer = new Uri($"{SiteOrigin}/");
        request.Headers.Add("Origin", SiteOrigin);
        request.Headers.Add("remittance-distro-id", "app_id_core");
        if (deviceId is not null)
        {
            request.Headers.Add("Remitly-DeviceEnvironmentID", deviceId);
        }
        return await http.SendAsync(request, ct);
    }

    /// <summary>
    /// Returns the cached device ID, fetching it from the corridor's public converter page when needed. A failure
    /// here is logged and the estimate is attempted without the header rather than failing the provider outright.
    /// </summary>
    private async Task<string?> GetDeviceIdAsync(Corridor c, bool refresh, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_deviceId is not null && !refresh)
            {
                return _deviceId;
            }
            var pageUrl = $"{SiteOrigin}/{c.SendCountry.ToLowerInvariant()}/en/currency-converter/" +
                          $"{c.SendCurrency.ToLowerInvariant()}-to-{c.ReceiveCurrency.ToLowerInvariant()}-rate";
            try
            {
                using var response = await http.GetAsync(pageUrl, ct);
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync(ct);
                var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];
                _deviceId = RemitlyParser.ParseDeviceEnvironmentId(html, cookies) ?? _deviceId;
                if (_deviceId is null)
                {
                    logger.LogWarning("No Remitly device ID found on {Url}; calling the estimate without it", pageUrl);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                logger.LogWarning("Could not load {Url} for a Remitly device ID ({Message}); calling the estimate without it",
                    pageUrl, ex.Message);
            }
            return _deviceId;
        }
        finally
        {
            _gate.Release();
        }
    }
}
