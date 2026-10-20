using System.Text;
using System.Text.Json;
using AngleSharp.Html.Parser;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://ahaliaexchange.com/</c>: an inline script declares
/// <c>var cc_data = {"ajax_url":"…","inr":"25.71","pkr":"75.39","bdt":"33.38",…};</c> (units per 1 AED, keyed by
/// lower-case currency code) and writes each value into the "Bank Transfer Rate" table, whose static HTML figure is
/// older. The board is headed "Bank Transfer Rate"; it carries no update time. Verified 2026-10-20.
/// </summary>
internal static class AhaliaExchangeParser
{
    private const string Marker = "var cc_data";

    public static PostedRate Parse(string html, Corridor corridor, DateTime fetchedAt)
    {
        var document = new HtmlParser().ParseDocument(html);
        var script = document.QuerySelectorAll("script")
            .Select(s => s.TextContent)
            .FirstOrDefault(text => text.Contains(Marker, StringComparison.Ordinal))
            ?? throw new FormatException("Ahalia home page has no cc_data script");
        var start = script.IndexOf('{', script.IndexOf(Marker, StringComparison.Ordinal));
        if (start < 0)
        {
            throw new FormatException("Ahalia cc_data is not an object");
        }

        decimal rate;
        try
        {
            // Reads the one object literal and ignores the rest of the script after it.
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(script[start..]));
            using var data = JsonDocument.ParseValue(ref reader);
            rate = data.RootElement.Path(corridor.ReceiveCurrency.ToLowerInvariant()).GetFlexibleDecimal();
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Ahalia cc_data is not valid JSON: {ex.Message}", ex);
        }
        return PostedRates.Create("AhaliaExchange", corridor, rate, Labels.BankDeposit, fetchedAt);
    }
}
