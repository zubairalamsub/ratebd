using AngleSharp.Html.Parser;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://hadiexchange.ae/</c>: the home page's "Money Transfer Rate" converter lists each destination as
/// <c>&lt;div class="option option_currency" data-type="BDT" data-rate="33.30"&gt;</c>, units per 1 AED, which the
/// converter multiplies by. The page names no payout method and carries no update time. Verified 2026-10-20.
/// </summary>
internal static class HadiExpressParser
{
    public static PostedRate Parse(string html, Corridor corridor, DateTime fetchedAt)
    {
        var document = new HtmlParser().ParseDocument(html);
        var option = document.QuerySelectorAll("[data-type][data-rate]")
            .FirstOrDefault(e => string.Equals(e.GetAttribute("data-type"), corridor.ReceiveCurrency, StringComparison.OrdinalIgnoreCase))
            ?? throw new FormatException($"Hadi Express home page has no {corridor.ReceiveCurrency} data-rate");

        return PostedRates.Create("HadiExpress", corridor, JsonExtensions.ParseDecimal(option.GetAttribute("data-rate")),
            Labels.NotSpecified, fetchedAt);
    }
}
