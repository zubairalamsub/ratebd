using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Mustafa Foreign Exchange (Singapore): the "Draft Rates" block on the mustafa.com.sg home page. Remittances are sent
/// at the counter; the page publishes no fee, no payout method and no real update time (its "Last Updated On" line is
/// the server clock). Verified 2026-10-20.
/// </summary>
public sealed class MustafaProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string SiteUrl = "https://www.mustafa.com.sg/";

    public string Name => "Mustafa";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SG" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        // The draft rates are quoted per Singapore dollar.
        if (c.SendCurrency != "SGD" || c.ReceiveCurrency != "BDT")
        {
            throw new ProviderNotSupportedException("Mustafa is only collected for SGD→BDT");
        }
        var html = await http.GetStringAsync(SiteUrl, ct);
        return [MustafaParser.Parse(html, c, "Bangladesh Taka", time.GetUtcNow().UtcDateTime)];
    }
}
