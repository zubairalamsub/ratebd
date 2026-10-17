using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Remitly's public calculator endpoint (used by remitly.com's send-money pages). It rate-limits bursts with
/// HTTP 429, so requests are sent one at a time with a pause, and Polly backs off further on 429.
/// </summary>
public sealed class RemitlyProvider(HttpClient http, TimeProvider time) : IRateProvider
{
    internal const string EstimateUrl = "https://api.remitly.io/v3/calculator/estimate";

    internal static readonly TimeSpan PauseBetweenRequests = TimeSpan.FromSeconds(3);

    // Remitly conduits use ISO 3166-1 alpha-3 country codes.
    private static readonly Dictionary<string, string> Alpha3 = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GB"] = "GBR",
        ["AE"] = "ARE",
        ["BD"] = "BGD",
    };

    public string Name => "Remitly";

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        if (!Alpha3.TryGetValue(c.SendCountry, out var from) || !Alpha3.TryGetValue(c.ReceiveCountry, out var to))
        {
            throw new ProviderNotSupportedException($"No Remitly conduit mapping for {c.SendCountry}→{c.ReceiveCountry}");
        }

        var quotes = new List<Quote>();
        foreach (var amount in sendAmounts)
        {
            if (quotes.Count > 0)
            {
                await Task.Delay(PauseBetweenRequests, time, ct);
            }
            var conduit = Uri.EscapeDataString($"{from}:{c.SendCurrency}-{to}:{c.ReceiveCurrency}");
            var url = $"{EstimateUrl}?conduit={conduit}&anchor=SEND&amount={amount:0.##}" +
                      "&purpose=OTHER&customer_segment=UNRECOGNIZED&strict_promo=false";
            using var response = await http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            quotes.Add(RemitlyParser.Parse(json, c, amount, time.GetUtcNow().UtcDateTime));
        }
        return quotes;
    }
}
