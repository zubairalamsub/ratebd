using System.Text.Json;
using System.Text.RegularExpressions;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>Parses responses from <c>GET https://api.remitly.com/v3/calculator/estimate</c> and the device ID on Remitly pages.</summary>
internal static partial class RemitlyParser
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

        // The headline estimate is Remitly's default payout for the corridor (bank deposit from the UK, cash pickup
        // from the USA and Italy). Bank deposit is used wherever it is offered so corridors compare like with like;
        // the alternatives share the same shape as the headline estimate.
        var candidates = new List<JsonElement> { root.Path("estimate") };
        if (root.OptionalPath("pay_out_price_estimates", "estimates") is { ValueKind: JsonValueKind.Array } alternatives)
        {
            candidates.AddRange(alternatives.EnumerateArray());
        }
        var estimate = candidates.FirstOrDefault(e => e.OptionalPath("pay_out_method").GetStringOrNull() == "BANK_DEPOSIT");
        if (estimate.ValueKind != JsonValueKind.Object)
        {
            estimate = candidates[0];
        }
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

    /// <summary>
    /// Remitly answers HTTP 400 <c>LEGACY_MERCHANDISING_BAD_REQUEST</c> ("unsupported corridor") for conduits it does
    /// not serve; that is reported as not supported rather than as a failure.
    /// </summary>
    public static void ThrowIfUnsupportedCorridor(string json, Corridor corridor)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array
                && doc.RootElement.EnumerateArray().Any(e =>
                    e.ValueKind == JsonValueKind.Object
                    && e.OptionalPath("error_key").GetStringOrNull() == "LEGACY_MERCHANDISING_BAD_REQUEST"))
            {
                throw new ProviderNotSupportedException(
                    $"Remitly does not offer {corridor.SendCountry}→{corridor.ReceiveCountry} ({corridor.SendCurrency}→{corridor.ReceiveCurrency})");
            }
        }
        catch (JsonException)
        {
            // Not a Remitly error body; the caller reports the HTTP status instead.
        }
    }

    /// <summary>
    /// Extracts the server-issued device ID that remitly.com pages embed as <c>"deviceEnvironmentId":"…"</c> in their
    /// page context, falling back to the <c>de_id</c> cookie the same response sets.
    /// </summary>
    public static string? ParseDeviceEnvironmentId(string html, IEnumerable<string> setCookieHeaders)
    {
        var match = DeviceIdRegex().Match(html);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        foreach (var header in setCookieHeaders)
        {
            var cookie = DeIdCookieRegex().Match(header);
            if (cookie.Success)
            {
                return cookie.Groups[1].Value;
            }
        }
        return null;
    }

    [GeneratedRegex("\"deviceEnvironmentId\":\"([A-Za-z0-9_-]{16,})\"")]
    private static partial Regex DeviceIdRegex();

    [GeneratedRegex(@"(?:^|;\s*)de_id=([A-Za-z0-9_-]{16,})")]
    private static partial Regex DeIdCookieRegex();
}
