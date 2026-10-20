using System.Globalization;
using AngleSharp.Html.Parser;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://www.lariexchange.com/ExchangeRates?Currency=BDT</c>: the "Exchange Rates" converter on
/// lariexchange.com, about 1.9 MB because the header's rate carousel is repeated some 30 times. With "I want to:
/// Transfer Money" (<c>tt_sell</c>) selected, the orange box reads "1 AED 33.55 BDT"
/// (<c>#MainContent_txtExchangeRateDisplay</c>, two decimals; the hidden <c>#MainContent_txtExchangeRate</c> holds the
/// AED-per-unit inverse). The carousel's first item is "LAST UPDATED 20/10/2026 13:50" (dd/MM/yyyy HH:mm, UAE time,
/// UTC+4, no DST), which trails the clock rather than echoing it. The page calls its rates indicative and excluding
/// fees; no payout method is named. Verified 2026-10-20.
/// </summary>
internal static class LariExchangeParser
{
    private static readonly TimeZoneInfo Dubai = TimeZoneInfo.CreateCustomTimeZone("Asia/Dubai", TimeSpan.FromHours(4), "Gulf", "Gulf");

    public static PostedRate Parse(string html, Corridor corridor, DateTime fetchedAt)
    {
        var document = new HtmlParser().ParseDocument(html);

        var currency = document.GetElementById("MainContent_lblFCurrency")?.TextContent.Trim();
        if (currency != corridor.ReceiveCurrency)
        {
            throw new FormatException($"Lari Exchange page shows {currency ?? "no currency"}, not {corridor.ReceiveCurrency}");
        }
        var transaction = document.QuerySelector("#MainContent_drpTransactionType option[selected]")?.GetAttribute("value");
        if (transaction != "tt_sell")
        {
            throw new FormatException($"Lari Exchange page shows '{transaction}' rates, not Transfer Money (tt_sell)");
        }
        var rateText = document.GetElementById("MainContent_txtExchangeRateDisplay")?.TextContent
            ?? throw new FormatException("Lari Exchange page has no exchange-rate display");

        // "LAST UPDATED" heads a two-row table; the stamp is the first cell of the next row. Without a readable date the
        // rate's age is unknown, so it is not published as current.
        var label = document.QuerySelectorAll("td")
            .FirstOrDefault(td => td.TextContent.Trim().Equals("LAST UPDATED", StringComparison.OrdinalIgnoreCase));
        var stampText = label?.ParentElement?.NextElementSibling?.QuerySelector("td")?.TextContent.Trim();
        if (!DateTime.TryParseExact(stampText, "d/M/yyyy H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var stamp))
        {
            throw new FormatException($"Lari Exchange page has no readable LAST UPDATED time (found '{stampText}')");
        }
        var published = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(stamp, DateTimeKind.Unspecified), Dubai);

        return PostedRates.Create("LariExchange", corridor, JsonExtensions.ParseDecimal(rateText), Labels.NotSpecified, fetchedAt,
            publishedAt: published);
    }
}
