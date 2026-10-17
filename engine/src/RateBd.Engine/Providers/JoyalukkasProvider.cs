using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Joyalukkas Exchange: the converter on joyalukkasexchange.com reads its TT rate from this API, per region
/// (1 Kuwait, 3 Oman, as listed in the site's own region data). No fee is published for these regions.
/// Verified 2026-10-17. Region 2 (UAE) also answers but is not collected yet.
/// </summary>
public sealed class JoyalukkasProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string ConverterUrl = "https://admin.joyalukkasexchange.com/api/currency-converter";

    private static readonly Dictionary<string, int> Regions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KW"] = 1,
        ["OM"] = 3,
    };

    public string Name => "Joyalukkas";

    public IReadOnlySet<string> SendCountries { get; } = Regions.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        if (!Regions.TryGetValue(c.SendCountry, out var region))
        {
            throw new ProviderNotSupportedException($"Joyalukkas rates are not collected from {c.SendCountry}");
        }
        var url = $"{ConverterUrl}?region={region}&amount=1000&currency_code={c.ReceiveCurrency}&rate_type=TT&amount_type=LCY";
        return [JoyalukkasParser.Parse(await http.GetStringAsync(url, ct), c, time.GetUtcNow().UtcDateTime)];
    }
}
