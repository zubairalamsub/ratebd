using System.Text.Json.Nodes;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Xe Money Transfer quotes from the unauthenticated endpoint used by xe.com's send-money widget.
/// <para>
/// NOTE: every response carries the comment "Automatic extraction of rates is prohibited under the Terms of Use."
/// The project owner chose to include Xe regardless; disable it with <c>Providers:Xe:Enabled=false</c>
/// (or <c>RATEBD_Providers__Xe__Enabled=false</c>) to stop all requests to Xe.
/// </para>
/// </summary>
public sealed class XeProvider(HttpClient http, TimeProvider time) : IRateProvider
{
    internal const string QuotesUrl = "https://launchpad-api.xe.com/v2/quotes";

    /// <summary>
    /// Send countries of the tracked corridors whose residents can use Xe Money Transfer. xe.com/send-money's
    /// <c>countryData</c> (checked 2026-10-17) marks GB, US and IT with <c>isXemtSupported: true</c>, and AE, SA, KW,
    /// QA, OM, MY and SG with <c>false</c>. The quote API still prices some of the latter (AED, KWD, QAR, SGD), but
    /// that is not a product their residents can buy.
    /// </summary>
    private static readonly HashSet<string> SupportedSendCountries = new(StringComparer.OrdinalIgnoreCase) { "GB", "US", "IT" };

    public string Name => "Xe";

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        if (!SupportedSendCountries.Contains(c.SendCountry))
        {
            throw new ProviderNotSupportedException(
                $"Xe Money Transfer is not available to senders in {c.SendCountry} (isXemtSupported=false on xe.com)");
        }

        var quotes = new List<Quote>();
        foreach (var amount in sendAmounts)
        {
            var body = new JsonObject
            {
                ["amount"] = amount,
                ["buyCcy"] = c.ReceiveCurrency,
                ["sellCcy"] = c.SendCurrency,
                ["userCountry"] = c.SendCountry,
                ["fixedCcy"] = c.SendCurrency,
                ["countryTo"] = c.ReceiveCountry,
            };
            using var response = await http.PostAsync(QuotesUrl, body.ToContent(), ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                XeParser.ThrowIfCurrencyNotAvailable(json, c);
            }
            response.EnsureSuccessStatusCode();
            quotes.Add(XeParser.Parse(json, c, amount, time.GetUtcNow().UtcDateTime));
        }
        return quotes;
    }
}
