using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Paysend's public corridor page, which embeds the calculator's rate and fee tariff in its server-rendered HTML.
/// One GET per corridor covers every amount. Only the page is read; robots.txt disallows <c>/api/</c>, which is not
/// called. Paysend sends from Kuwait but not from Saudi Arabia, Qatar or Oman (checked 2026-10-17).
/// </summary>
public sealed class PaysendProvider(HttpClient http, TimeProvider time) : IRateProvider, ISendCountryScoped
{
    // URL slugs Paysend uses for its corridor pages.
    private static readonly Dictionary<string, string> Slugs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KW"] = "kuwait",
        ["BD"] = "bangladesh",
    };

    public string Name => "Paysend";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KW" };

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        if (!Slugs.TryGetValue(c.SendCountry, out var from) || !Slugs.TryGetValue(c.ReceiveCountry, out var to))
        {
            throw new ProviderNotSupportedException($"Paysend quotes are not collected for {c.SendCountry}→{c.ReceiveCountry}");
        }
        var html = await http.GetStringAsync($"https://paysend.com/en/send-money/from-{from}-to-{to}", ct);
        return PaysendParser.Parse(html, c, sendAmounts, time.GetUtcNow().UtcDateTime);
    }
}
