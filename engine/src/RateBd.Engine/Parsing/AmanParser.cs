using System.Globalization;
using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://portal.amankuwait.com/amsweb/api/rate</c>, the rate board on amankuwait.com. In <c>CRList[]</c>,
/// <c>KDAmt</c> is units per 1 KWD for transfers ("KD Eqv" next to "Sell Transfer" = <c>KDRate</c>);
/// <c>BRate</c>/<c>SRate</c> are cash-note rates. <c>RateDate</c> is Kuwait local time (UTC+3) without an offset.
/// </summary>
internal static class AmanParser
{
    private static readonly TimeSpan KuwaitOffset = TimeSpan.FromHours(3);

    public static PostedRate Parse(string json, Corridor corridor, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var row = root.Path("CRList").EnumerateArray()
            .FirstOrDefault(r => r.OptionalPath("Code").GetStringOrNull() == corridor.ReceiveCurrency);
        if (row.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"Aman rate board has no {corridor.ReceiveCurrency} row");
        }

        // Without a readable date the board's age is unknown, so the rate is not published as current.
        var text = root.OptionalPath("RateDate").GetStringOrNull();
        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var stamp))
        {
            throw new FormatException($"Aman rate board has no readable RateDate (found '{text}')");
        }
        // Today's format has no offset (Kuwait local time); honour one if the board ever adds it.
        var published = stamp.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(stamp - KuwaitOffset, DateTimeKind.Utc)
            : stamp.ToUniversalTime();

        var rate = Math.Round(row.Path("KDAmt").GetFlexibleDecimal(), 4, MidpointRounding.AwayFromZero);
        // "Transfer" without a payout method; online sending needs an Aman account, so this is the branch rate.
        return PostedRates.Create("Aman", corridor, rate, Labels.NotSpecified, fetchedAt, publishedAt: published);
    }
}
