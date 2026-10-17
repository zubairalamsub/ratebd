using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// Parses <c>POST https://mmapi.ooredoo.qa:4443/ooredoo-smartphone/jaxrs/spmpoint/getIRDeliveryOptionList</c>, the
/// list ooredoomoney.qa's send-money page shows: one rate per partner exchange house and payout method, with
/// free-text speed and fee range. The rate does not depend on the amount.
/// </summary>
internal static class OoredooMoneyParser
{
    // Listed bank first so that, on equal rates, a partner's bank-deposit rate is the one kept.
    private static readonly (string List, string Delivery)[] Lists =
    [
        ("accountDOList", Labels.BankDeposit),
        ("walletDOList", Labels.MobileWallet),
        ("cashDOList", Labels.CashPickup),
    ];

    public static IReadOnlyList<PostedRate> Parse(string json, Corridor corridor, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var code = root.OptionalPath("status", "code").GetStringOrNull();
        if (code != "0")
        {
            throw new FormatException($"Ooredoo Money returned status {code ?? "(none)"}: {root.OptionalPath("status", "value").GetStringOrNull()}");
        }
        DateTime? updated = root.OptionalPath("lastUpdatedDate") is { ValueKind: JsonValueKind.Number } ms
            ? DateTimeOffset.FromUnixTimeMilliseconds(ms.GetInt64()).UtcDateTime
            : null;

        var options = Lists
            .SelectMany(l => root.OptionalPath(l.List) is { ValueKind: JsonValueKind.Array } items
                ? items.EnumerateArray().Select(item => (Item: item, l.Delivery)).ToList()
                : [])
            .Select(x => (
                Partner: x.Item.Path("exchangeHouseDesc").GetString() ?? "",
                Rate: x.Item.Path("fxRate").GetFlexibleDecimal(),
                Delivery: WithAgent(x.Delivery, x.Item.OptionalPath("receiveAgentAbbr").GetStringOrNull()),
                Speed: x.Item.OptionalPath("deliverySpeed").GetStringOrNull(),
                Fee: x.Item.OptionalPath("feeRange").GetStringOrNull()))
            .ToList();
        if (options.Count == 0)
        {
            throw new FormatException("Ooredoo Money listed no payout options");
        }

        // One row per partner: its best rate across payout methods (OrderBy is stable, so bank wins ties).
        return options
            .GroupBy(o => o.Partner, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(o => o.Rate).First())
            .Select(o => PostedRates.Create("OoredooMoney", corridor, o.Rate, o.Delivery, fetchedAt,
                speed: string.IsNullOrWhiteSpace(o.Speed) ? Labels.NotSpecified : o.Speed.Trim(),
                partner: o.Partner,
                feeNote: string.IsNullOrWhiteSpace(o.Fee) ? null : o.Fee.Trim(),
                publishedAt: updated))
            .ToList();
    }

    /// <summary>"Mobile wallet (bKash)"; catch-alls such as "All Wallets" add nothing.</summary>
    private static string WithAgent(string delivery, string? agent) => agent?.Trim() switch
    {
        null or "" => delivery,
        var a when a.StartsWith("All ", StringComparison.OrdinalIgnoreCase) => delivery,
        var a when a.Equals("bkash", StringComparison.OrdinalIgnoreCase) => $"{delivery} (bKash)",
        var a => $"{delivery} ({a})",
    };
}
