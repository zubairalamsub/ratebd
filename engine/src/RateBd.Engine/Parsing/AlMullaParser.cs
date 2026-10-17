using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>
/// Parses <c>POST https://app-kwt.almullaexchange.com/pub/xtrate/get</c>, the quote almullaexchange.com's calculator
/// shows. <c>results[0].dynamicRoutingPricingList</c> groups routes (correspondent bank and service) under
/// NO_BENE_DEDUCT (fee paid by the sender on top) and BENE_DEDUCT (fee taken from the recipient's amount; empty when
/// captured). Each route has its own rate, fee and delivery estimate.
/// </summary>
internal static class AlMullaParser
{
    public static Quote Parse(string json, Corridor corridor, decimal sendAmount, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var status = root.OptionalPath("statusKey").GetStringOrNull();
        if (status != "SUCCESS")
        {
            throw new FormatException($"Al Mulla returned status {status ?? "(none)"}: {root.OptionalPath("message").GetStringOrNull()}");
        }

        var routes = root.Path("results").EnumerateArray().Take(1)
            .SelectMany(r => r.Path("dynamicRoutingPricingList").EnumerateArray())
            // Only fee-on-top routes: BENE_DEDUCT would need a different normalisation and has never been populated.
            .SelectMany(group => group.EnumerateObject().Where(p => p.Name == "NO_BENE_DEDUCT").SelectMany(p => p.Value.EnumerateArray()))
            .Select(q => (
                Rate: q.Path("exRateBreakup", "domXRate").GetFlexibleDecimal(),
                Fee: q.Path("txnFee").GetFlexibleDecimal(),
                Seconds: q.OptionalPath("trnxRoutingPaths", "estimatedDeliveryDetails", "processTimeTotalInSeconds"),
                Delivery: q.OptionalPath("trnxRoutingPaths", "deliveryDescription").GetStringOrNull()))
            .ToList();
        if (routes.Count == 0)
        {
            throw new FormatException("Al Mulla returned no routes");
        }

        // Every route is a price Al Mulla offers; take the best rate (ties go to the cheaper fee). The calculator itself
        // preselects the fastest route ("selection": "MIN_TIME"), which can be slightly cheaper for the recipient.
        var best = routes.OrderByDescending(r => r.Rate).ThenBy(r => r.Fee).First();
        var speed = best.Seconds is { ValueKind: JsonValueKind.Number } s && s.GetInt64() > 0
            ? Labels.Within(TimeSpan.FromSeconds(s.GetInt64()))
            : Labels.NotSpecified;

        // "BANKING CHANNEL" (every route seen so far) is a deposit to a Bangladeshi bank account.
        var delivery = best.Delivery switch
        {
            "BANKING CHANNEL" => Labels.BankDeposit,
            null or "" => Labels.NotSpecified,
            var other => other,
        };
        return new Quote("AlMulla", corridor.Id, corridor.SendCurrency, sendAmount, best.Rate, best.Fee,
            QuoteMath.ReceiveFor(sendAmount, best.Fee, best.Rate), delivery, speed, fetchedAt, Stale: false);
    }
}
