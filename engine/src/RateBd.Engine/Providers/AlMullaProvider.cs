using System.Text.Json.Nodes;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Al Mulla Exchange (Kuwait). The calculator on almullaexchange.com posts a form with one field, <c>body</c>, holding
/// a JSON quote request; the answer prices every correspondent route with its fee (added on top) and delivery time.
/// The API reports <c>X-Rate-Limit-Remaining</c>, so the provider pauses between amounts. Verified 2026-10-17.
/// </summary>
public sealed class AlMullaProvider(HttpClient http, TimeProvider time) : IRateProvider, ISendCountryScoped
{
    internal const string QuoteUrl = "https://app-kwt.almullaexchange.com/pub/xtrate/get";

    internal static readonly TimeSpan PauseBetweenRequests = TimeSpan.FromSeconds(2);

    // Al Mulla's own ids for the destination, as its calculator sends them.
    private const int BangladeshCountryId = 97;
    private const int TakaCurrencyId = 7;

    public string Name => "AlMulla";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KW" };

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        if (c.ReceiveCurrency != "BDT")
        {
            throw new ProviderNotSupportedException($"Al Mulla is only collected for {c.SendCurrency}→BDT");
        }

        var quotes = new List<Quote>();
        foreach (var amount in sendAmounts)
        {
            if (quotes.Count > 0)
            {
                await Task.Delay(PauseBetweenRequests, time, ct);
            }
            var quoteRequest = new JsonObject
            {
                ["foreignCountryId"] = BangladeshCountryId,
                ["foreignCurrencyId"] = TakaCurrencyId,
                ["quoteName"] = "BDT",
                ["localAmount"] = amount,
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, QuoteUrl)
            {
                Content = new FormUrlEncodedContent([new("body", quoteRequest.ToJsonString())]),
            };
            request.Headers.Add("Origin", "https://www.almullaexchange.com");
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            quotes.Add(AlMullaParser.Parse(await response.Content.ReadAsStringAsync(ct), c, amount, time.GetUtcNow().UtcDateTime));
        }
        return quotes;
    }
}
