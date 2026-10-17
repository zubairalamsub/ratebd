using AngleSharp.Html.Parser;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://almirqabexchange.com/view_rates.jsp</c>: an HTML table COUNTRY | CURRENCY | BUY | SALE | DRAFT/TT.
/// BUY and SALE are cash-note rates (QAR per unit); DRAFT/TT is the remittance rate in units per QAR.
/// </summary>
internal static class AlMirqabParser
{
    public static PostedRate Parse(string html, Corridor corridor, DateTime fetchedAt)
    {
        var document = new HtmlParser().ParseDocument(html);
        foreach (var table in document.QuerySelectorAll("table"))
        {
            var headers = table.QuerySelectorAll("thead th").Select(th => th.TextContent.Trim()).ToList();
            var currencyIndex = headers.FindIndex(h => h.Equals("CURRENCY", StringComparison.OrdinalIgnoreCase));
            var ttIndex = headers.FindIndex(h => h.Equals("DRAFT/TT", StringComparison.OrdinalIgnoreCase));
            if (currencyIndex < 0 || ttIndex < 0)
            {
                continue;
            }
            var row = table.QuerySelectorAll("tbody tr")
                .Select(tr => tr.QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToList())
                .FirstOrDefault(cells => cells.Count > ttIndex && cells[currencyIndex] == corridor.ReceiveCurrency)
                ?? throw new FormatException($"Al Mirqab rate table has no {corridor.ReceiveCurrency} row");
            return PostedRates.Create("AlMirqab", corridor, JsonExtensions.ParseDecimal(row[ttIndex]), Labels.BankDeposit,
                fetchedAt);
        }
        throw new FormatException("Al Mirqab page has no table with CURRENCY and DRAFT/TT columns");
    }
}
