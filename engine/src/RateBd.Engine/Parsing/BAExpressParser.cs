using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>
/// Parses <c>POST https://www.baexpress.net/WebServices/WebService.asmx/Load_ExchangeRate_And_Fee</c>, the calculator
/// on baexpress.net. The web service wraps its answer as a JSON string in <c>d</c>, so it is parsed twice. The inner
/// <c>Table[0]</c> has <c>ExRate</c> (BDT per USD), <c>ServiceFee</c> and <c>HandlingFee</c> (USD; the page shows only
/// the service fee and converts the whole amount sent, so the fee comes on top) and <c>PaymentMethodName</c>
/// ("e-Check", how the sender pays, not the payout). No payout method or delivery time is given. Verified 2026-10-20.
/// </summary>
internal static class BAExpressParser
{
    public static Quote Parse(string json, Corridor corridor, decimal sendAmount, DateTime fetchedAt)
    {
        using var outer = JsonDocument.Parse(json);
        var inner = outer.RootElement.OptionalPath("d").GetStringOrNull()
            ?? throw new FormatException("BA Express answer has no 'd' string");
        using var doc = JsonDocument.Parse(inner);

        // An empty table is what the page shows as "--NOT FOUND--".
        var row = doc.RootElement.Path("Table").EnumerateArray().FirstOrDefault();
        if (row.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"BA Express returned no rate for {corridor.SendCurrency} {sendAmount}");
        }

        var rate = row.Path("ExRate").GetFlexibleDecimal();
        if (rate <= 0)
        {
            throw new FormatException($"Implausible BA Express rate {rate} for {corridor.Id}");
        }
        var fee = row.Path("ServiceFee").GetFlexibleDecimal() + row.Path("HandlingFee").GetFlexibleDecimal();
        return new Quote("BAExpress", corridor.Id, corridor.SendCurrency, sendAmount, rate, fee,
            QuoteMath.ReceiveFor(sendAmount, fee, rate), Labels.NotSpecified, Labels.NotSpecified, fetchedAt, Stale: false);
    }
}
