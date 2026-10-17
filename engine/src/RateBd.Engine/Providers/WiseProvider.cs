using System.Text.Json.Nodes;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Wise's public, unauthenticated quote endpoint (the same one wise.com's calculator uses).
/// AED→BDT returns only a disabled BALANCE pay-in option, and currencies Wise cannot send from (SAR, KWD, QAR, OMR)
/// return HTTP 422 route.not.supported; both are reported as not supported.
/// </summary>
public sealed class WiseProvider(HttpClient http, TimeProvider time) : IRateProvider
{
    internal const string QuotesUrl = "https://api.wise.com/v3/quotes/";

    public string Name => "Wise";

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        var quotes = new List<Quote>();
        foreach (var amount in sendAmounts)
        {
            var body = new JsonObject
            {
                ["sourceCurrency"] = c.SendCurrency,
                ["targetCurrency"] = c.ReceiveCurrency,
                ["sourceAmount"] = amount,
            };
            using var response = await http.PostAsync(QuotesUrl, body.ToContent(), ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                WiseParser.ThrowIfRouteNotSupported(json, c);
            }
            response.EnsureSuccessStatusCode();
            quotes.Add(WiseParser.Parse(json, c, amount, time.GetUtcNow().UtcDateTime));
        }
        return quotes;
    }
}
