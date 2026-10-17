using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Al Ansari Exchange (Kuwait): the converter on alansariexchange.com.kw. A plain GET of the home page gives the
/// anonymous WordPress nonce the converter call needs (it expires within a day, so it is read each run). The site
/// labels the rate indicative and publishes no fee. Verified 2026-10-17.
/// </summary>
public sealed class AlAnsariProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string HomeUrl = "https://alansariexchange.com.kw/";
    internal const string AjaxUrl = "https://alansariexchange.com.kw/wp-admin/admin-ajax.php";

    // The site's own currency ids: 22 = KWD, 59 = BDT (Bangladesh), from the converter's data-* attributes.
    private const string KuwaitiDinarId = "22";
    private const string BangladeshId = "59";

    public string Name => "AlAnsari";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KW" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        if (c.SendCurrency != "KWD" || c.ReceiveCurrency != "BDT")
        {
            throw new ProviderNotSupportedException($"Al Ansari is only collected for KWD→BDT");
        }
        var nonce = AlAnsariParser.ParseNonce(await http.GetStringAsync(HomeUrl, ct));
        using var request = new HttpRequestMessage(HttpMethod.Post, AjaxUrl)
        {
            Content = new FormUrlEncodedContent(
            [
                new("action", "convert_action"),
                new("currfrom", KuwaitiDinarId),
                new("currto", BangladeshId),
                new("cntcode", BangladeshId),
                new("amt", "1"),
                new("security", nonce),
                new("trtype", "BT"),
            ]),
        };
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Referrer = new Uri(HomeUrl);
        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        // WordPress answers a rejected nonce with 403 and "-1", which the parser explains; anything else is an HTTP error.
        if (!response.IsSuccessStatusCode && body.Trim() != "-1")
        {
            throw new HttpRequestException($"Al Ansari converter answered HTTP {(int)response.StatusCode}");
        }
        return [AlAnsariParser.Parse(body, c, time.GetUtcNow().UtcDateTime)];
    }
}
