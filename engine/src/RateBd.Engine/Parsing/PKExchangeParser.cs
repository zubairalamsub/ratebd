using System.Globalization;
using AngleSharp.Html.Parser;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://www.pkexchange.com.om/rates/</c>: "Today's Exchange Rates" with the date (dd/MM/yyyy) under the
/// heading and a table Country | Remittance (TT, Cash Pay) | FOREX (Buy, Sell). TT is the bank-transfer rate.
/// </summary>
internal static class PKExchangeParser
{
    private static readonly TimeZoneInfo Muscat = TimeZoneInfo.CreateCustomTimeZone("Asia/Muscat", TimeSpan.FromHours(4), "Gulf", "Gulf");

    public static PostedRate Parse(string html, Corridor corridor, string countryName, DateTime fetchedAt)
    {
        var document = new HtmlParser().ParseDocument(html);
        var table = document.QuerySelectorAll("table")
            .FirstOrDefault(t => t.QuerySelectorAll("thead th").Any(th => th.TextContent.Trim() == "TT"))
            ?? throw new FormatException("PK Exchange rates table not found");

        // The second header row lists the value columns (blank, TT, Cash Pay, Buy, Sell); body rows start with the country.
        var columns = table.QuerySelectorAll("thead tr").Last().QuerySelectorAll("th").Select(th => th.TextContent.Trim()).ToList();
        var ttIndex = columns.IndexOf("TT");
        var row = table.QuerySelectorAll("tbody tr")
            .Select(tr => tr.QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToList())
            .FirstOrDefault(cells => cells.Count > ttIndex && cells[0].Equals(countryName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FormatException($"PK Exchange has no {countryName} row");

        // The page is dated (Oman time) but carries no time; treat the date as midnight there. Without a readable date
        // the rate's age is unknown, so it is not published as current.
        var dateText = document.QuerySelectorAll("h2")
            .FirstOrDefault(h => h.TextContent.Contains("Exchange Rates", StringComparison.OrdinalIgnoreCase))
            ?.NextElementSibling?.TextContent.Trim();
        if (!DateTime.TryParseExact(dateText, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new FormatException($"PK Exchange rates page has no readable date (found '{dateText}')");
        }
        var published = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date, DateTimeKind.Unspecified), Muscat);

        return PostedRates.Create("PKExchange", corridor, JsonExtensions.ParseDecimal(row[ttIndex]), Labels.BankDeposit, fetchedAt,
            publishedAt: published);
    }
}
