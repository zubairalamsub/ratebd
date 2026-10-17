using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>
/// Parses <c>POST https://transfers.skrill.com/api/transfers/v4/marketing/preview</c>, the quote Skrill's public
/// money-transfer calculator shows. All amounts are decimal strings.
/// </summary>
internal static class SkrillParser
{
    public static Quote Parse(string json, Corridor corridor, decimal sendAmount, string deliveryMethod, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var amounts = doc.RootElement.Path("amounts");
        var send = amounts.Path("send").GetFlexibleDecimal();
        if (send != sendAmount)
        {
            throw new FormatException($"Skrill quoted {send} {corridor.SendCurrency} instead of {sendAmount}");
        }
        var rate = amounts.Path("fxRate").GetFlexibleDecimal();
        var fee = amounts.Path("fee").GetFlexibleDecimal();
        var receive = amounts.Path("receive").GetFlexibleDecimal();

        // "receive" is send × rate: the fee (0 when captured) is added on top, so normalise to "pays sendAmount in total".
        // discounts[] (e.g. FTC, a first-transfer promotion) are ignored, as for Remitly's promo price.
        var normalised = fee == 0 ? receive : QuoteMath.ReceiveFor(sendAmount, fee, rate);

        return new Quote("Skrill", corridor.Id, corridor.SendCurrency, sendAmount, rate, fee, normalised, deliveryMethod,
            Speed(doc.RootElement.OptionalPath("deliveryTimes")), fetchedAt, Stale: false);
    }

    /// <summary><c>{"type":"INSTANT"}</c> or <c>{"type":"UP_TO","upperBound":{"amount":1,"timeUnit":"DAYS"}}</c>.</summary>
    private static string Speed(JsonElement? deliveryTimes)
    {
        if (deliveryTimes is not { } times)
        {
            return Labels.NotSpecified;
        }
        switch (times.OptionalPath("type").GetStringOrNull())
        {
            case "INSTANT":
                return "Instant";
            case "UP_TO" when times.OptionalPath("upperBound") is { } bound:
                var amount = bound.Path("amount").GetFlexibleDecimal();
                var unit = bound.OptionalPath("timeUnit").GetStringOrNull();
                TimeSpan? span = unit switch
                {
                    "MINUTES" => TimeSpan.FromMinutes((double)amount),
                    "HOURS" => TimeSpan.FromHours((double)amount),
                    "DAYS" => TimeSpan.FromDays((double)amount),
                    _ => null,
                };
                return span is { } s ? Labels.Within(s) : Labels.NotSpecified;
            default:
                return Labels.NotSpecified;
        }
    }
}
