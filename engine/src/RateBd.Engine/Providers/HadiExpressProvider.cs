using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Hadi Express Exchange (UAE): the "Money Transfer Rate" converter on the hadiexchange.ae home page, which carries
/// each destination's rate in a <c>data-rate</c> attribute. The page calls its rates indicative and "updated every 24
/// hours" but gives no update time, and no fee is published with them. Verified 2026-10-20.
/// </summary>
public sealed class HadiExpressProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string SiteUrl = "https://hadiexchange.ae/";

    public string Name => "HadiExpress";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AE" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        // The converter only converts from AED.
        if (c.SendCurrency != "AED" || c.ReceiveCurrency != "BDT")
        {
            throw new ProviderNotSupportedException("Hadi Express is only collected for AED→BDT");
        }
        var html = await http.GetStringAsync(SiteUrl, ct);
        return [HadiExpressParser.Parse(html, c, time.GetUtcNow().UtcDateTime)];
    }
}
