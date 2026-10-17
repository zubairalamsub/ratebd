using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://admin.joyalukkasexchange.com/api/currency-converter?region=N&amp;currency_code=BDT&amp;rate_type=TT…</c>,
/// the converter on joyalukkasexchange.com. <c>data[0].RATE</c> is the TT (bank transfer) rate as send currency per
/// BDT, e.g. "0.0024945340" KWD.
/// </summary>
internal static class JoyalukkasParser
{
    public static PostedRate Parse(string json, Corridor corridor, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        // "Exchange Rate 1 BDT = KWD 0.0024945340": guards against a renumbered region quoting another currency.
        var label = doc.RootElement.OptionalPath("exchange_rate").GetStringOrNull() ?? "";
        if (!label.Contains($"= {corridor.SendCurrency} ", StringComparison.Ordinal))
        {
            throw new FormatException($"Joyalukkas quoted '{label}', not {corridor.SendCurrency}");
        }
        var row = doc.RootElement.Path("data").EnumerateArray()
            .FirstOrDefault(r => r.OptionalPath("CURRENCY_CODE").GetStringOrNull() == corridor.ReceiveCurrency);
        if (row.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"Joyalukkas returned no {corridor.ReceiveCurrency} rate");
        }
        return PostedRates.Create("Joyalukkas", corridor, PostedRates.Invert(row.Path("RATE").GetFlexibleDecimal()),
            Labels.BankDeposit, fetchedAt);
    }
}
