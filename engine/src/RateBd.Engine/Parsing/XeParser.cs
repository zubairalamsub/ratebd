using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>Parses responses from <c>POST https://launchpad-api.xe.com/v2/quotes</c>.</summary>
internal static class XeParser
{
    /// <summary>
    /// Picks Xe's default enabled option (or else the enabled option that delivers the most). Amount fields arrive
    /// as strings with thousands separators. Xe adds its fee on top, so the quote is normalised to "fee included".
    /// </summary>
    public static Quote Parse(string json, Corridor corridor, decimal sendAmount, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var quote = doc.RootElement.Path("quote");
        var enabled = quote.Path("individualQuotes").EnumerateArray()
            .Where(q => q.OptionalPath("isEnabled") is { ValueKind: JsonValueKind.True })
            .ToList();
        if (enabled.Count == 0)
        {
            var errors = quote.OptionalPath("errorMessages")?.GetRawText() ?? "none";
            throw new FormatException($"Xe returned no enabled quote options (errorMessages: {errors})");
        }

        var defaults = enabled.Where(q => q.OptionalPath("isDefault") is { ValueKind: JsonValueKind.True }).ToList();
        var chosen = defaults.Count > 0 ? defaults[0] : enabled.MaxBy(q => q.Path("buyAmount").GetFlexibleDecimal());

        var rate = chosen.Path("rate").GetFlexibleDecimal();
        var fee = chosen.Path("totalFees").GetFlexibleDecimal();
        var delivery = chosen.OptionalPath("deliveryMethod").GetStringOrNull() switch
        {
            "BankAccount" => Labels.BankDeposit,
            "CashPayout" => Labels.CashPickup,
            "MobileWallet" => Labels.MobileWallet,
            { Length: > 0 } other => other,
            _ => Labels.NotSpecified,
        };
        var speed = chosen.OptionalPath("leadTime").GetStringOrNull() is { Length: > 0 } lead ? lead : Labels.NotSpecified;

        return new Quote("Xe", corridor.Id, corridor.SendCurrency, sendAmount, rate, fee,
            Providers.QuoteMath.ReceiveFor(sendAmount, fee, rate), delivery, speed, fetchedAt, Stale: false);
    }
}
