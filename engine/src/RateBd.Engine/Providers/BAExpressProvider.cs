using System.Globalization;
using System.Text.Json.Nodes;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// BA Express USA (Bank Asia's US remittance company): the "Show Exchange Rate and Fee" calculator on baexpress.net,
/// which posts the send amount to the site's own web service and shows the rate and service fee. The fee steps up with
/// the amount and is charged on top of it, so the provider asks once per amount and pauses in between.
/// Verified 2026-10-20.
/// </summary>
public sealed class BAExpressProvider(HttpClient http, TimeProvider time) : IRateProvider, ISendCountryScoped
{
    internal const string QuoteUrl = "https://www.baexpress.net/WebServices/WebService.asmx/Load_ExchangeRate_And_Fee";

    /// <summary>The page whose calculator script posts to <see cref="QuoteUrl"/>.</summary>
    internal const string PageUrl = "https://www.baexpress.net/";

    internal static readonly TimeSpan PauseBetweenRequests = TimeSpan.FromSeconds(2);

    public string Name => "BAExpress";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "US" };

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        // The calculator script always sends USD.
        if (c.SendCurrency != "USD" || c.ReceiveCurrency != "BDT")
        {
            throw new ProviderNotSupportedException("BA Express is only collected for USD→BDT");
        }

        var quotes = new List<Quote>();
        foreach (var amount in sendAmounts)
        {
            if (quotes.Count > 0)
            {
                await Task.Delay(PauseBetweenRequests, time, ct);
            }
            // The same body the calculator script sends: the amount as a string with two decimals.
            var quoteRequest = new JsonObject
            {
                ["p_SendingCurrency"] = c.SendCurrency,
                ["p_SendingAmount"] = amount.ToString("0.00", CultureInfo.InvariantCulture),
                ["p_FxCurrency"] = c.ReceiveCurrency,
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, QuoteUrl) { Content = quoteRequest.ToContent() };
            request.Headers.Referrer = new Uri(PageUrl);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            quotes.Add(BAExpressParser.Parse(await response.Content.ReadAsStringAsync(ct), c, amount, time.GetUtcNow().UtcDateTime));
        }
        return quotes;
    }
}
