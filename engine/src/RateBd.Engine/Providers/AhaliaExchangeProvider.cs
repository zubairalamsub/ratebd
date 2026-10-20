using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Ahalia Exchange (UAE): the "Bank Transfer Rate" board on the ahaliaexchange.com home page. The board's HTML holds
/// an older figure that the page's script replaces from an inline <c>cc_data</c> object, so the rate is read from that
/// object, as visitors see it. The page calls its rates indicative, gives no update time and publishes no fee.
/// Verified 2026-10-20.
/// </summary>
public sealed class AhaliaExchangeProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string SiteUrl = "https://ahaliaexchange.com/";

    public string Name => "AhaliaExchange";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AE" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        // The board and calculator only convert from AED.
        if (c.SendCurrency != "AED" || c.ReceiveCurrency != "BDT")
        {
            throw new ProviderNotSupportedException("Ahalia Exchange is only collected for AED→BDT");
        }
        var html = await http.GetStringAsync(SiteUrl, ct);
        return [AhaliaExchangeParser.Parse(html, c, time.GetUtcNow().UtcDateTime)];
    }
}
