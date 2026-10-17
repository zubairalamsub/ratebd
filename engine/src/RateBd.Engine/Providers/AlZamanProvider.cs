using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Al Zaman Exchange (Qatar): the rate widget on alzamanexchange.com.qa reads its TT rate from this endpoint
/// (cid 6 is BDT in the site's currency list). No fee is published. Verified 2026-10-17.
/// </summary>
public sealed class AlZamanProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string RateUrl = "https://alzamanexchange.com/api/getRate.php?cid=6";

    public string Name => "AlZaman";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "QA" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        if (c.ReceiveCurrency != "BDT")
        {
            throw new ProviderNotSupportedException("Al Zaman is only collected for BDT");
        }
        return [AlZamanParser.Parse(await http.GetStringAsync(RateUrl, ct), c, time.GetUtcNow().UtcDateTime)];
    }
}
