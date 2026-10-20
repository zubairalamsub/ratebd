using System.Globalization;
using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://www.orientexchange.com/Orient/GetExchangeRates</c> (JSON served as text/html), the "Transfer Rates"
/// table on orientexchange.com's Currency Rates page. In <c>rateList[]</c>, <c>Rate</c> is foreign units per AED as a
/// string ("33.6501"; rates below 1 drop the leading zero, ".2717") and <c>LastUpdatedOnDateFormattedString</c>
/// ("20 Oct 2026 02.07 PM") is UAE time (UTC+4, no DST); the top-level <c>lastUpdatedOnDate</c> repeats it. Only
/// <c>rateList</c> is read. The stamp is a real update time, not the server clock: it stayed put across fetches minutes
/// apart. No payout method is named and no fee is in the response. Verified 2026-10-20.
/// </summary>
internal static class OrientExchangeParser
{
    private static readonly TimeZoneInfo Dubai = TimeZoneInfo.CreateCustomTimeZone("Asia/Dubai", TimeSpan.FromHours(4), "Gulf", "Gulf");

    // Seen zero-padded ("02.07 PM"); an unpadded hour is accepted too.
    private static readonly string[] StampFormats = ["d MMM yyyy hh.mm tt", "d MMM yyyy h.mm tt"];

    public static PostedRate Parse(string json, Corridor corridor, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var row = root.Path("rateList").EnumerateArray()
            .FirstOrDefault(r => r.OptionalPath("CurrencyCode").GetStringOrNull() == corridor.ReceiveCurrency);
        if (row.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"Orient Exchange transfer rates have no {corridor.ReceiveCurrency} row");
        }

        // Without a readable date the rate's age is unknown, so it is not published as current.
        var text = row.OptionalPath("LastUpdatedOnDateFormattedString").GetStringOrNull()
            ?? root.OptionalPath("lastUpdatedOnDate").GetStringOrNull();
        if (!DateTime.TryParseExact(text?.Trim(), StampFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var stamp))
        {
            throw new FormatException($"Orient Exchange {corridor.ReceiveCurrency} row has no readable update time (found '{text}')");
        }
        var published = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(stamp, DateTimeKind.Unspecified), Dubai);

        return PostedRates.Create("OrientExchange", corridor, row.Path("Rate").GetFlexibleDecimal(), Labels.NotSpecified, fetchedAt,
            publishedAt: published);
    }
}
