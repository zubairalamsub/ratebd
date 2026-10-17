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
    /// Countries whose residents can use Xe Money Transfer. xe.com/send-money's <c>countryData</c> marks GB with
    /// <c>isXemtSupported: true</c> and AE with <c>isXemtSupported: false</c>; the quote API still prices AED, but
    /// that is not a product a UAE resident can buy.
    /// </summary>
    private static readonly HashSet<string> SupportedSendCountries = new(StringComparer.OrdinalIgnoreCase) { "GB" };

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
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            quotes.Add(XeParser.Parse(json, c, amount, time.GetUtcNow().UtcDateTime));
        }
        return quotes;
    }
}
