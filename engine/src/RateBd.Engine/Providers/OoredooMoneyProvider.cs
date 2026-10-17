using System.Globalization;
using System.Text.Json.Nodes;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Ooredoo Money (Qatar mobile wallet). ooredoomoney.qa's send-money page lists, for each partner exchange house
/// (MoneyGram, Western Union, Al Zaman, …), its rate per payout method, with speed and a fee range ("Starting from
/// QAR 20") but no exact fee. One call per run covers every partner; all body fields are required. Verified 2026-10-17.
/// </summary>
public sealed class OoredooMoneyProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    // Port 4443 is part of the address the page itself calls.
    internal const string DeliveryOptionsUrl = "https://mmapi.ooredoo.qa:4443/ooredoo-smartphone/jaxrs/spmpoint/getIRDeliveryOptionList";
    internal const string SiteOrigin = "https://www.ooredoomoney.qa";

    public string Name => "OoredooMoney";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "QA" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        if (c.ReceiveCountry != "BD")
        {
            throw new ProviderNotSupportedException($"Ooredoo Money is only collected for transfers to Bangladesh");
        }
        var body = new JsonObject
        {
            ["origin"] = "mapp",
            ["traceNo"] = time.GetUtcNow().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            ["AuditData"] = new JsonObject
            {
                ["device"] = "web",
                ["deviceId"] = "browser",
                ["otherDeviceId"] = "",
                ["application"] = "Webflow",
                ["applicationVersion"] = "1.0",
                ["location"] = "QA",
                ["deviceIMEI"] = "",
            },
            ["countryCode"] = "BGD",
            ["currencyCode"] = c.ReceiveCurrency,
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, DeliveryOptionsUrl) { Content = body.ToContent() };
        request.Headers.Add("Origin", SiteOrigin);
        request.Headers.Referrer = new Uri(SiteOrigin + "/");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return OoredooMoneyParser.Parse(await response.Content.ReadAsStringAsync(ct), c, time.GetUtcNow().UtcDateTime);
    }
}
