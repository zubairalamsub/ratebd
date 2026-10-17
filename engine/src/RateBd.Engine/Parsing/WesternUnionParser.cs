using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>
/// Parses <c>POST https://www.westernunion.com/wuconnect/prices/catalog</c>, the price catalogue westernunion.com's
/// send-money form calls. <c>services_groups[]</c> holds one entry per payout method (MM cash pickup, DB bank,
/// MB mobile wallet), each with <c>pay_groups[]</c> priced per pay-in method.
/// </summary>
internal static class WesternUnionParser
{
    /// <summary>
    /// The price segment westernunion.com visitors get. Requests without the page's <c>wu_device_id</c> cookie are
    /// answered from a different tier (no segment, other rates and fees) that the site never shows, so anything else
    /// is rejected rather than published.
    /// </summary>
    internal const string WebsiteSegment = "N00";

    public static Quote Parse(string json, Corridor corridor, decimal sendAmount, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = root.Path("response_status");
        var code = status.Path("code").GetString();
        if (code != "P0000")
        {
            throw new FormatException(
                $"Western Union price catalogue returned {code}: {status.OptionalPath("message").GetStringOrNull()}");
        }
        var segment = root.Path("sender").OptionalPath("segment").GetStringOrNull();
        if (segment != WebsiteSegment)
        {
            throw new FormatException(
                $"Western Union priced the request in segment '{segment ?? "none"}', not the website segment {WebsiteSegment}");
        }

        var options = root.Path("services_groups").EnumerateArray()
            .Select(group =>
            {
                // Pay-in "CA" (cash at an agent) is the only one offered to guests; take it, else the first listed.
                var payGroups = group.Path("pay_groups").EnumerateArray().ToList();
                var pay = payGroups.FirstOrDefault(p => p.OptionalPath("fund_in").GetStringOrNull() == "CA");
                if (pay.ValueKind == JsonValueKind.Undefined)
                {
                    pay = payGroups.FirstOrDefault();
                }
                return (Group: group, Pay: pay);
            })
            .Where(x => x.Pay.ValueKind == JsonValueKind.Object)
            .Select(x =>
            {
                // The fee is charged on top (gross_amount = send_amount + gross_fee); normalise to "pays sendAmount in total".
                var fee = x.Pay.Path("gross_fee").GetFlexibleDecimal();
                var rate = x.Pay.Path("fx_rate").GetFlexibleDecimal();
                return (Method: x.Group.Path("fund_out_mnem").GetString(), Name: x.Group.OptionalPath("service_name").GetStringOrNull(),
                    Days: x.Group.OptionalPath("speed_days"), Fee: fee, Rate: rate,
                    Receive: QuoteMath.ReceiveFor(sendAmount, fee, rate));
            })
            .ToList();
        if (options.Count == 0)
        {
            throw new FormatException("Western Union returned no payout options");
        }

        // Bank deposit when offered (as for the other providers), otherwise whatever delivers the most.
        var best = options.Any(o => o.Method == "DB")
            ? options.First(o => o.Method == "DB")
            : options.MaxBy(o => o.Receive);

        var delivery = best.Method switch
        {
            "DB" => Labels.BankDeposit,
            "MB" => Labels.MobileWallet,
            "MM" => Labels.CashPickup,
            _ => best.Name ?? Labels.NotSpecified,
        };
        var speed = best.Days is { ValueKind: JsonValueKind.Number } days && days.GetInt32() > 0
            ? Labels.Within(TimeSpan.FromDays(days.GetInt32()))
            : Labels.NotSpecified;

        return new Quote("WesternUnion", corridor.Id, corridor.SendCurrency, sendAmount, best.Rate, best.Fee, best.Receive,
            delivery, speed, fetchedAt, Stale: false);
    }
}
