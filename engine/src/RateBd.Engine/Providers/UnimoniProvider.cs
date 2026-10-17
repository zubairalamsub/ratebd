using System.Globalization;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Unimoni (Oman): the live-rate converter on unimoni.om. The rate is flat across amounts, so one call for 1,000 OMR
/// gives it to four decimals. The service fee is charged on top and not published. Verified 2026-10-17.
/// </summary>
public sealed class UnimoniProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string RateUrl = "https://www.unimoni.om/crm/live/rate";
    internal const decimal QuotedAmount = 1000m;

    public string Name => "Unimoni";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OM" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RateUrl)
        {
            Content = new FormUrlEncodedContent([new("code", c.ReceiveCurrency), new("amount", QuotedAmount.ToString(CultureInfo.InvariantCulture))]),
        };
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Referrer = new Uri("https://www.unimoni.om/");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        // JSON served as text/html.
        var json = await response.Content.ReadAsStringAsync(ct);
        return [UnimoniParser.Parse(json, c, QuotedAmount, time.GetUtcNow().UtcDateTime)];
    }
}
