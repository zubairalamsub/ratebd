using System.Globalization;
using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://gccexchange.com/exchangerate/getExchangeRate</c>, the feed behind the "Calculate Rate" widget on
/// gccexchange.com. In <c>result.exchangerate[]</c>, <c>ExchangeRate</c> is AED per foreign unit (BDT
/// "0.029722404040773"); the widget shows the inverse to four decimals ("1 AED = 33.6447 BDT"), which is what
/// <see cref="PostedRates.Invert"/> gives. <c>LastModifiedDate</c> ("2026-10-20 14:00:06") is set per row in UAE time
/// (UTC+4, no DST). It is when the row was last written, not the server clock: a refresh rewrote the BDT row at 14:00:06
/// and again at 14:15:08 with the same rate, while the SAR row still said 2026-08-26. The widget's "Fee: AED 0.00" is
/// hard-coded in its script, so no fee is read. Verified 2026-10-20.
/// </summary>
internal static class GccExchangeParser
{
    private static readonly TimeZoneInfo Dubai = TimeZoneInfo.CreateCustomTimeZone("Asia/Dubai", TimeSpan.FromHours(4), "Gulf", "Gulf");

    public static PostedRate Parse(string json, Corridor corridor, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.Path("result");
        var status = result.Path("result");
        // The widget's own check: String(result.result) === "1".
        if ((status.ValueKind == JsonValueKind.String ? status.GetString() : status.GetRawText()) != "1")
        {
            throw new FormatException($"GCC Exchange returned result {status.GetRawText()}");
        }

        // The widget keys its rates by currency code, so a later row for the same currency wins; do the same.
        var row = result.Path("exchangerate").EnumerateArray()
            .LastOrDefault(r => r.OptionalPath("CurrencyCode").GetStringOrNull() == corridor.ReceiveCurrency);
        if (row.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"GCC Exchange rate feed has no {corridor.ReceiveCurrency} row");
        }

        // Without a readable date the rate's age is unknown, so it is not published as current.
        var text = row.OptionalPath("LastModifiedDate").GetStringOrNull();
        if (!DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var stamp))
        {
            throw new FormatException($"GCC Exchange {corridor.ReceiveCurrency} row has no readable LastModifiedDate (found '{text}')");
        }
        var published = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(stamp, DateTimeKind.Unspecified), Dubai);

        // ServiceName is "ALL" for BDT; only an explicit "BANK TRANSFER" names a payout method.
        var delivery = row.OptionalPath("ServiceName").GetStringOrNull() == "BANK TRANSFER" ? Labels.BankDeposit : Labels.NotSpecified;
        var rate = PostedRates.Invert(row.Path("ExchangeRate").GetFlexibleDecimal());
        return PostedRates.Create("GccExchange", corridor, rate, delivery, fetchedAt, publishedAt: published);
    }
}
