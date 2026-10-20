using AngleSharp.Html.Parser;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://www.sonaliexchange.com/rates.asp</c>: "Conversion Rate: 1 USD = … Taka" and a calculator whose
/// read-only <c>&lt;input id="aspRate"&gt;</c> holds the same rate (BDT per USD). The page carries no date, its
/// commission table is commented out and it names no payout method. (Sonali's terms, §J, say no fee is currently
/// charged to the sender; that is not on this page, so no fee note is set.) Verified 2026-10-20.
/// </summary>
internal static class SonaliExchangeParser
{
    public static PostedRate Parse(string html, Corridor corridor, DateTime fetchedAt)
    {
        var document = new HtmlParser().ParseDocument(html);
        var value = document.QuerySelector("input#aspRate")?.GetAttribute("value")
            ?? throw new FormatException("Sonali Exchange rates page has no aspRate field");
        return PostedRates.Create("SonaliExchange", corridor, JsonExtensions.ParseDecimal(value), Labels.NotSpecified, fetchedAt);
    }
}
