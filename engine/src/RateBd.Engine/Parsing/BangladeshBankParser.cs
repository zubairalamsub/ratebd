using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using RateBd.Engine.Json;

namespace RateBd.Engine.Parsing;

/// <summary>USD/BDT weighted average rate from Bangladesh Bank's "Exchange Rate of Taka" page.</summary>
internal sealed record BangladeshBankRate(decimal UsdBdt, string? EffectiveDate);

/// <summary>
/// Parses <c>https://www.bb.org.bd/en/index.php/econdata/exchangerate</c>. The rate sits in a class-less table whose
/// first header reads "A. USD/BDT Exchange Rate" with columns Currency | Bid Rate | Ask Rate | WAR.
/// </summary>
internal static partial class BangladeshBankParser
{
    public static BangladeshBankRate Parse(string html)
    {
        var document = new HtmlParser().ParseDocument(html);

        var table = document.QuerySelectorAll("table")
            .FirstOrDefault(t => t.QuerySelector("th")?.TextContent.Contains("USD/BDT Exchange Rate", StringComparison.OrdinalIgnoreCase) == true);
        if (table is null)
        {
            var challenged = html.Contains("Please enable JavaScript", StringComparison.OrdinalIgnoreCase);
            throw new FormatException(challenged
                ? "Bangladesh Bank returned its JavaScript bot challenge instead of the rate page (run tools/bb-snapshot)"
                : "USD/BDT table not found on the Bangladesh Bank page");
        }

        var headers = table.QuerySelectorAll("thead tr").Last().QuerySelectorAll("th")
            .Select(th => th.TextContent.Trim())
            .ToList();
        var warIndex = headers.FindIndex(h => h.Equals("WAR", StringComparison.OrdinalIgnoreCase));
        if (warIndex < 0)
        {
            throw new FormatException($"WAR column not found (headers: {string.Join(", ", headers)})");
        }

        var usdRow = table.QuerySelectorAll("tbody tr")
            .Select(tr => tr.QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToList())
            .FirstOrDefault(cells => cells.Count > warIndex && cells[0].Equals("USD", StringComparison.OrdinalIgnoreCase))
            ?? throw new FormatException("USD row not found in the USD/BDT table");

        var rate = JsonExtensions.ParseDecimal(usdRow[warIndex]);
        if (rate <= 0)
        {
            throw new FormatException($"Implausible USD/BDT rate '{usdRow[warIndex]}'");
        }

        return new BangladeshBankRate(rate, ParseEffectiveDate(document.Body?.TextContent ?? ""));
    }

    private static string? ParseEffectiveDate(string text)
    {
        var match = EffectiveDateRegex().Match(text);
        return match.Success && DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
    }

    [GeneratedRegex(@"effective for (\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex EffectiveDateRegex();
}
