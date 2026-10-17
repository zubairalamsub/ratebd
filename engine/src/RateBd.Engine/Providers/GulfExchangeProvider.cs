using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Gulf Exchange (Qatar). Its converter needs the session cookie and CSRF token that a plain GET of the home page
/// issues; the typed client keeps cookies (the default handler's CookieContainer). The rate does not depend on the
/// amount and no fee is published. Verified 2026-10-17.
/// </summary>
public sealed class GulfExchangeProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string HomeUrl = "https://gulfexchange.com.qa/en";
    internal const string ConvertUrl = "https://gulfexchange.com.qa/en/convert-currency";

    public string Name => "GulfExchange";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "QA" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        var token = GulfExchangeParser.ParseToken(await http.GetStringAsync(HomeUrl, ct));
        using var request = new HttpRequestMessage(HttpMethod.Post, ConvertUrl)
        {
            Content = new FormUrlEncodedContent(
            [
                new("_token", token),
                new("currency_from", c.SendCurrency),
                new("currency_to", c.ReceiveCurrency),
                new("payment_method", "bank_transfer"),
                new("amountType", "L"),
                new("countryCode", c.ReceiveCountry),
                new("send_amt", "1000"), // flat rate: the amount only scales the receive figure
            ]),
        };
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Referrer = new Uri(HomeUrl);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return [GulfExchangeParser.Parse(await response.Content.ReadAsStringAsync(ct), c, time.GetUtcNow().UtcDateTime)];
    }
}
