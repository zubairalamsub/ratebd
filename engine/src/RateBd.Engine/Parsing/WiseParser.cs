using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>Parses responses from <c>POST https://api.wise.com/v3/quotes/</c>.</summary>
internal static class WiseParser
{
    public static Quote Parse(string json, Corridor corridor, decimal sendAmount, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var rate = root.Path("rate").GetDecimal();

        var options = root.Path("paymentOptions").EnumerateArray().ToList();
        var enabled = options.Where(o => !o.Path("disabled").GetBoolean()).ToList();
        if (enabled.Count == 0)
        {
            var reasons = options
                .Select(o => o.OptionalPath("disabledReason", "code").GetStringOrNull())
                .Where(r => r is not null)
                .Distinct();
            throw new ProviderNotSupportedException(
                $"Wise offers no fundable pay-in method for {corridor.SendCurrency}→{corridor.ReceiveCurrency} " +
                $"(disabled: {string.Join(", ", reasons)})");
        }

        // A regular bank transfer is Wise's standard way to pay; fall back to the cheapest enabled option.
        var bankTransfer = enabled.Where(o => o.Path("payIn").GetString() == "BANK_TRANSFER").ToList();
        var option = bankTransfer.Count > 0
            ? bankTransfer[0]
            : enabled.MinBy(o => o.Path("fee", "total").GetDecimal());

        // Wise deducts its fee from the source amount, so targetAmount already matches our convention.
        var fee = option.Path("fee", "total").GetDecimal();
        var receive = option.Path("targetAmount").GetDecimal();

        var speed = Labels.NotSpecified;
        if (option.OptionalPath("estimatedDelivery").GetStringOrNull() is { } eta
            && DateTime.TryParse(eta, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var etaUtc))
        {
            var created = root.OptionalPath("createdTime").GetStringOrNull() is { } c
                          && DateTime.TryParse(c, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var createdUtc)
                ? createdUtc
                : fetchedAt;
            speed = Labels.Within(etaUtc - created);
        }

        var delivery = option.Path("payOut").GetString() switch
        {
            "BANK_TRANSFER" => Labels.BankDeposit,
            { Length: > 0 } other => other,
            _ => Labels.NotSpecified,
        };

        return new Quote("Wise", corridor.Id, corridor.SendCurrency, sendAmount, rate, fee, receive,
            delivery, speed, fetchedAt, Stale: false);
    }
}
