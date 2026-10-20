using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Orient Exchange (UAE): the dated "Transfer Rates" table on orientexchange.com's Currency Rates page, read from the
/// JSON it loads. No fee is in the response. Verified 2026-10-20.
/// </summary>
public sealed class OrientExchangeProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string PageUrl = "https://www.orientexchange.com/Orient/CurrencyRates";
    internal const string RateUrl = "https://www.orientexchange.com/Orient/GetExchangeRates";

    public string Name => "OrientExchange";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AE" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, RateUrl);
        request.Headers.Referrer = new Uri(PageUrl);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        // JSON served as text/html.
        return [OrientExchangeParser.Parse(await response.Content.ReadAsStringAsync(ct), c, time.GetUtcNow().UtcDateTime)];
    }
}
