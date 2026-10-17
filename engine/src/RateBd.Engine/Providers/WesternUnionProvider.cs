using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Western Union's price catalogue, called the way the send-money form on westernunion.com/{sa|kw|qa} calls it.
/// <para>
/// The page's script gives every visitor a random <c>wu_device_id</c> cookie (generated with Math.random in the
/// browser, never issued by the server). Without one the API answers from a price tier the site never shows, so the
/// provider does what a browser does: one random ID per run. The parser rejects any response outside the website
/// price segment.
/// </para>
/// <para>
/// Collected for Saudi Arabia, Kuwait and Qatar only (verified 2026-10-17). Western Union's Oman site has no online
/// calculator, and its other countries are covered by the providers already tracked there.
/// westernunion.com is behind Akamai bot management; a 403 or HTML answer is reported as a failure.
/// </para>
/// </summary>
public sealed class WesternUnionProvider(HttpClient http, TimeProvider time) : IRateProvider, ISendCountryScoped
{
    internal const string CatalogUrl = "https://www.westernunion.com/wuconnect/prices/catalog";
    internal const string SiteOrigin = "https://www.westernunion.com";

    internal static readonly TimeSpan PauseBetweenRequests = TimeSpan.FromSeconds(3);

    // The PROD_DIGITAL client id each country's home page passes to the catalogue (amerigo_settings in the page).
    private static readonly Dictionary<string, string> Clients = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SA"] = "ARA100016",
        ["KW"] = "AWD280010",
        ["QA"] = "AQA270013",
    };

    private readonly string _deviceId = Guid.NewGuid().ToString("D");

    public string Name => "WesternUnion";

    public IReadOnlySet<string> SendCountries { get; } = Clients.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        if (!Clients.TryGetValue(c.SendCountry, out var client))
        {
            throw new ProviderNotSupportedException($"Western Union quotes are not collected from {c.SendCountry}");
        }

        var quotes = new List<Quote>();
        foreach (var amount in sendAmounts)
        {
            if (quotes.Count > 0)
            {
                await Task.Delay(PauseBetweenRequests, time, ct);
            }
            var json = await PostCatalogAsync(c, client, amount, ct);
            quotes.Add(WesternUnionParser.Parse(json, c, amount, time.GetUtcNow().UtcDateTime));
        }
        return quotes;
    }

    private async Task<string> PostCatalogAsync(Corridor c, string client, decimal amount, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["header_request"] = new JsonObject { ["version"] = "0.5", ["request_type"] = "PRICECATALOG" },
            ["sender"] = new JsonObject
            {
                ["client"] = client,
                ["channel"] = "WRET",
                ["funds_in"] = "*",
                ["curr_iso3"] = c.SendCurrency,
                ["cty_iso2_ext"] = c.SendCountry,
                ["send_amount"] = amount.ToString("0.00", CultureInfo.InvariantCulture),
            },
            ["receiver"] = new JsonObject
            {
                ["curr_iso3"] = c.ReceiveCurrency,
                ["cty_iso2_ext"] = c.ReceiveCountry,
                ["cty_iso2"] = c.ReceiveCountry,
            },
        };

        // The page posts the JSON as text/plain.
        using var request = new HttpRequestMessage(HttpMethod.Post, CatalogUrl)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "text/plain"),
        };
        request.Headers.Add("Cookie", $"wu_device_id={_deviceId}");
        request.Headers.Add("Origin", SiteOrigin);
        request.Headers.Referrer = new Uri($"{SiteOrigin}/{c.SendCountry.ToLowerInvariant()}/en/home.html");

        using var response = await http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        var isJson = text.TrimStart().StartsWith('{');
        if (!response.IsSuccessStatusCode || !isJson)
        {
            throw new HttpRequestException(
                $"Western Union answered HTTP {(int)response.StatusCode}{(isJson ? "" : " with a non-JSON page (bot protection?)")}");
        }
        return text;
    }
}
