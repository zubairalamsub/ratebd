using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>Parses <c>GET https://api.taptapsend.com/api/fxRates</c>, which lists one rate per corridor for every send country.</summary>
internal static class TaptapSendParser
{
    public static IReadOnlyList<Quote> Parse(string json, Corridor corridor, decimal[] sendAmounts, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var sender = doc.RootElement.Path("availableCountries").EnumerateArray()
            .Where(c => c.Path("isoCountryCode").GetString() == corridor.SendCountry
                        && c.Path("currency").GetString() == corridor.SendCurrency)
            .Select(c => (JsonElement?)c)
            .FirstOrDefault()
            ?? throw new ProviderNotSupportedException($"Taptap Send does not list {corridor.SendCountry} ({corridor.SendCurrency}) as a send country");

        var target = sender.Path("corridors").EnumerateArray()
            .Where(c => c.Path("isoCountryCode").GetString() == corridor.ReceiveCountry
                        && c.Path("currency").GetString() == corridor.ReceiveCurrency)
            .Select(c => (JsonElement?)c)
            .FirstOrDefault()
            ?? throw new ProviderNotSupportedException($"Taptap Send has no {corridor.SendCountry}→{corridor.ReceiveCountry} corridor");

        var rate = target.Path("fxRate").GetFlexibleDecimal();

        // The API gives one rate per corridor (not per amount) and no delivery method or delivery time.
        return sendAmounts
            .Select(amount =>
            {
                var fee = FeeFor(target, amount);
                return new Quote("TaptapSend", corridor.Id, corridor.SendCurrency, amount, rate, fee,
                    QuoteMath.ReceiveFor(amount, fee, rate), Labels.NotSpecified, Labels.NotSpecified, fetchedAt, Stale: false);
            })
            .ToList();
    }

    /// <summary>
    /// Fees come from <c>feeSchedule</c> ("standard" flat fee or "tiered" by minimum send amount) or
    /// <c>senderCurrencyFlatFee</c>. Corridors that carry neither (both GB→BD and AE→BD at the time of writing)
    /// are treated as fee-free.
    /// </summary>
    internal static decimal FeeFor(JsonElement corridor, decimal amount)
    {
        if (corridor.OptionalPath("feeSchedule") is { } schedule)
        {
            switch (schedule.OptionalPath("type").GetStringOrNull())
            {
                case "standard":
                    return schedule.Path("flatFee").GetFlexibleDecimal();
                case "tiered":
                    return schedule.Path("tiers").EnumerateArray()
                        .Select(t => (Min: t.Path("minValue").GetFlexibleDecimal(), Fee: t.Path("fee").GetFlexibleDecimal()))
                        .Where(t => t.Min <= amount)
                        .OrderByDescending(t => t.Min)
                        .Select(t => t.Fee)
                        .FirstOrDefault();
                default:
                    throw new FormatException($"Unknown Taptap Send fee schedule type in {schedule.GetRawText()}");
            }
        }
        return corridor.OptionalPath("senderCurrencyFlatFee") is { } flat ? flat.GetFlexibleDecimal() : 0m;
    }
}
