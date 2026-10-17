using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Purshottam Kanji Exchange (Oman): the dated "Today's Exchange Rates" page on pkexchange.com.om, which labels its
/// rates indicative. The commission appears only on the receipt. Verified 2026-10-17.
/// </summary>
public sealed class PKExchangeProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string RatesUrl = "https://www.pkexchange.com.om/rates/";

    public string Name => "PKExchange";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OM" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        if (c.ReceiveCountry != "BD")
        {
            throw new ProviderNotSupportedException("PK Exchange is only collected for Bangladesh");
        }
        var html = await http.GetStringAsync(RatesUrl, ct);
        return [PKExchangeParser.Parse(html, c, "BANGLADESH", time.GetUtcNow().UtcDateTime)];
    }
}
