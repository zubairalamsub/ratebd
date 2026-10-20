using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Lari Exchange (UAE): the dated "Exchange Rates" converter on lariexchange.com, which labels its rates indicative and
/// excluding fees. The page is about 1.9 MB and has taken 4 to 30 s to arrive, but its headers come back in under a
/// second: the time goes on the body, which HttpClient buffers after the resilience pipeline has returned, so the
/// per-attempt and total timeouts don't bound it. The default budget therefore leaves the most room for the download
/// within <c>Engine:ProviderTimeoutSeconds</c>. Verified 2026-10-20.
/// </summary>
public sealed class LariExchangeProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string RatesUrl = "https://www.lariexchange.com/ExchangeRates";

    public string Name => "LariExchange";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AE" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        using var response = await http.GetAsync($"{RatesUrl}?Currency={Uri.EscapeDataString(c.ReceiveCurrency)}", ct);
        response.EnsureSuccessStatusCode();
        return [LariExchangeParser.Parse(await response.Content.ReadAsStringAsync(ct), c, time.GetUtcNow().UtcDateTime)];
    }
}
