using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>Parses responses from <c>GET https://api.remitly.io/v3/calculator/estimate</c>.</summary>
internal static class RemitlyParser
{
    /// <summary>
    /// Uses Remitly's standard ("everyday") price: <c>exchange_rate.base_rate</c> and the undiscounted
    /// <c>fee.total_fee_amount</c>. The response's <c>receive_amount</c> includes a one-off new-customer promo
    /// (boosted rate on a capped amount, fee waived), which is deliberately ignored.
    /// </summary>
    public static Quote Parse(string json, Corridor corridor, decimal sendAmount, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            // Errors come back as [{"error_key": "...", "message": "..."}].
            var first = root.EnumerateArray().FirstOrDefault();
            var key = first.ValueKind == JsonValueKind.Object ? first.OptionalPath("error_key").GetStringOrNull() : null;
            throw new FormatException($"Remitly returned an error: {key ?? root.GetRawText()}");
        }

        var estimate = root.Path("estimate");
        var rate = estimate.Path("exchange_rate", "base_rate").GetFlexibleDecimal();
        var fee = estimate.Path("fee", "total_fee_amount").GetFlexibleDecimal();

        var delivery = estimate.OptionalPath("pay_out_method").GetStringOrNull() switch
        {
            "BANK_DEPOSIT" => Labels.BankDeposit,
            "CASH_PICKUP" => Labels.CashPickup,
            "DIRECT_TO_PHONE" => Labels.MobileWallet,
            { Length: > 0 } other => other,
            _ => Labels.NotSpecified,
        };

        // The estimate has no delivery-time field, so speed is left unspecified rather than guessed.
        return new Quote("Remitly", corridor.Id, corridor.SendCurrency, sendAmount, rate, fee,
            QuoteMath.ReceiveFor(sendAmount, fee, rate), delivery, Labels.NotSpecified, fetchedAt, Stale: false);
    }
}
