using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://www.mustafa.com.sg/</c>: under the "Draft Rates" heading, each currency is an <c>h3</c> such as
/// "Bangladesh Taka" followed by a one-row table "Todays Rate | 96.5065" (units per 1 SGD). The "Last Updated On" text
/// beside it is rendered from the server clock on every request, so it is not a publish time and is ignored.
/// Verified 2026-10-20.
/// </summary>
internal static class MustafaParser
{
    public static PostedRate Parse(string html, Corridor corridor, string currencyName, DateTime fetchedAt)
    {
        var document = new HtmlParser().ParseDocument(html);
        var block = document.QuerySelectorAll("h3")
            .FirstOrDefault(h => Text(h).Equals("Draft Rates", StringComparison.OrdinalIgnoreCase))
            ?.ParentElement
            ?? throw new FormatException("Mustafa home page has no Draft Rates block");
        var heading = block.QuerySelectorAll("h3")
            .FirstOrDefault(h => Text(h).Equals(currencyName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FormatException($"Mustafa Draft Rates has no {currencyName} entry");

        var cells = heading.ParentElement?.QuerySelectorAll("table tr")
            .Select(tr => tr.QuerySelectorAll("td").Select(Text).ToList())
            .FirstOrDefault(row => row.Count >= 2 && row[0].Equals("Todays Rate", StringComparison.OrdinalIgnoreCase))
            ?? throw new FormatException($"Mustafa {currencyName} entry has no Todays Rate");

        return PostedRates.Create("Mustafa", corridor, JsonExtensions.ParseDecimal(cells[1]), Labels.NotSpecified, fetchedAt);
    }

    // Headings pad the name with &nbsp; after a flag image.
    private static string Text(IElement element) => element.TextContent.Replace(' ', ' ').Trim();
}
