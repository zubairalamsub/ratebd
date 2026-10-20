using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// GCC Exchange (UAE): the rate feed behind the "Calculate Rate" widget on gccexchange.com, which the site says is
/// updated every five minutes. One call returns every currency with its own update time; no fee is published.
/// Verified 2026-10-20.
/// </summary>
public sealed class GccExchangeProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string SiteUrl = "https://gccexchange.com/";
    internal const string RateUrl = "https://gccexchange.com/exchangerate/getExchangeRate";

    public string Name => "GccExchange";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AE" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        // The headers the widget's jQuery call sends from the home page.
        using var request = new HttpRequestMessage(HttpMethod.Get, RateUrl);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Referrer = new Uri(SiteUrl);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return [GccExchangeParser.Parse(await response.Content.ReadAsStringAsync(ct), c, time.GetUtcNow().UtcDateTime)];
    }
}
