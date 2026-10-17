using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

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
        // additionalTransferFee is charged on top of totalFees on some routes (e.g. 4.000 KWD, 55.00 QAR); it is 0.00
        // on the tracked Xe corridors today but is included so a route that adds it is never understated.
        var fee = chosen.Path("totalFees").GetFlexibleDecimal()
                  + (chosen.OptionalPath("additionalTransferFee") is { } extra ? extra.GetFlexibleDecimal() : 0m);
        var delivery = chosen.OptionalPath("deliveryMethod").GetStringOrNull() switch
        {
            "BankAccount" => Labels.BankDeposit,
            "CashPayout" => Labels.CashPickup,
            "MobileWallet" => Labels.MobileWallet,
            "PeerToPeer" => "Peer to peer",
            { Length: > 0 } other => other,
            _ => Labels.NotSpecified,
        };
        var speed = chosen.OptionalPath("leadTime").GetStringOrNull() is { Length: > 0 } lead ? lead : Labels.NotSpecified;

        return new Quote("Xe", corridor.Id, corridor.SendCurrency, sendAmount, rate, fee,
            QuoteMath.ReceiveFor(sendAmount, fee, rate), delivery, speed, fetchedAt, Stale: false);
    }

    /// <summary>
    /// Xe answers HTTP 400 with <c>errorMessages.QUOTE006</c> ("Currency not available online") for routes it does
    /// not offer; that is reported as not supported rather than as a failure.
    /// </summary>
    public static void ThrowIfCurrencyNotAvailable(string json, Corridor corridor)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.OptionalPath("quote", "errorMessages", "QUOTE006") is { } error)
            {
                throw new ProviderNotSupportedException(
                    $"Xe does not offer {corridor.SendCurrency}→{corridor.ReceiveCurrency} online " +
                    $"(QUOTE006 for {error.OptionalPath("ccy").GetStringOrNull()})");
            }
        }
        catch (JsonException)
        {
            // Not an Xe error body; the caller reports the HTTP status instead.
        }
    }
}
