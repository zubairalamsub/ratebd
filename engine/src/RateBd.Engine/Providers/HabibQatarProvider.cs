using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Habib Qatar International Exchange: the converter on hqie.com.qa. The rate does not depend on the amount and no
/// fee is published. The home page's slider can show a slightly different figure; the converter is used throughout.
/// Verified 2026-10-17.
/// </summary>
public sealed class HabibQatarProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string SiteUrl = "https://hqie.com.qa/";

    public string Name => "HabibQatar";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "QA" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{SiteUrl}Account/ConvertCurr?_Covertcountry={c.SendCurrency}&_CovertAmt=1000&_Covertedcountry={c.ReceiveCurrency}");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Referrer = new Uri(SiteUrl);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return [HabibQatarParser.Parse(await response.Content.ReadAsStringAsync(ct), c, time.GetUtcNow().UtcDateTime)];
    }
}
