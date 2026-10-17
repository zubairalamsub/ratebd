using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>One WorldRemit payout option for a corridor, e.g. MOB / bKash Mobile Money.</summary>
internal sealed record WorldRemitPayOutMethod(string Code, string CorrespondentId, string CorrespondentName, string Speed);

/// <summary>Parses responses from WorldRemit's public GraphQL API (<c>https://api.worldremit.com/graphql</c>).</summary>
internal static class WorldRemitParser
{
    /// <summary>Payout codes that are not money transfers (ATP = airtime top-up) and so are skipped.</summary>
    private static readonly HashSet<string> ExcludedCodes = ["ATP"];

    public static IReadOnlyList<WorldRemitPayOutMethod> ParsePayOutMethods(string json)
    {
        using var doc = JsonDocument.Parse(json);
        ThrowOnGraphQlErrors(doc.RootElement);

        var methods = new List<WorldRemitPayOutMethod>();
        foreach (var method in doc.RootElement.Path("data", "payOutMethods").EnumerateArray())
        {
            var code = method.Path("code").GetString()!;
            if (ExcludedCodes.Contains(code))
            {
                continue;
            }
            var estimate = method.OptionalPath("payOutTimeEstimate").GetStringOrNull();
            foreach (var correspondent in method.Path("correspondents").EnumerateArray().Take(1))
            {
                var speed = correspondent.OptionalPath("payOutTime").GetStringOrNull() ?? estimate;
                methods.Add(new WorldRemitPayOutMethod(
                    code,
                    correspondent.Path("id").GetString()!,
                    correspondent.Path("name").GetString()!,
                    CleanSpeed(speed)));
            }
        }
        return methods;
    }

    /// <summary>
    /// WorldRemit charges its fee on top of the amount sent, so the quote is normalised to "fee included".
    /// Promotional discounts are ignored to show the standard price.
    /// </summary>
    public static Quote ParseCalculation(string json, Corridor corridor, decimal sendAmount, WorldRemitPayOutMethod method,
        DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        ThrowOnGraphQlErrors(doc.RootElement);

        var result = doc.RootElement.Path("data", "createCalculation");
        var errors = result.OptionalPath("errors");
        if (errors is { ValueKind: JsonValueKind.Array } list && list.GetArrayLength() > 0)
        {
            var messages = list.EnumerateArray().Select(e =>
                $"{e.OptionalPath("__typename").GetStringOrNull()} " +
                $"{e.OptionalPath("genericType").GetStringOrNull() ?? e.OptionalPath("code").GetStringOrNull()}: " +
                $"{e.OptionalPath("message").GetStringOrNull()}");
            throw new FormatException($"WorldRemit calculation failed ({string.Join("; ", messages)})");
        }

        var calculation = result.Path("calculation");
        var rate = calculation.Path("exchangeRate", "value").GetFlexibleDecimal();
        var fee = calculation.Path("informativeSummary", "fee", "value", "amount").GetFlexibleDecimal();

        return new Quote("WorldRemit", corridor.Id, corridor.SendCurrency, sendAmount, rate, fee,
            QuoteMath.ReceiveFor(sendAmount, fee, rate), DeliveryLabel(method), method.Speed, fetchedAt, Stale: false);
    }

    private static string DeliveryLabel(WorldRemitPayOutMethod method) => method.Code switch
    {
        "MOB" => $"{Labels.MobileWallet} ({method.CorrespondentName.Replace(" Mobile Money", "", StringComparison.OrdinalIgnoreCase)})",
        "BNK" => Labels.BankDeposit,
        "CSH" => Labels.CashPickup,
        _ => method.CorrespondentName,
    };

    /// <summary>Strips decorative prefixes such as "⚡ " from WorldRemit's delivery estimate.</summary>
    private static string CleanSpeed(string? speed)
    {
        var trimmed = speed?.TrimStart(c => !char.IsLetterOrDigit(c)).Trim();
        return string.IsNullOrEmpty(trimmed) ? Labels.NotSpecified : trimmed;
    }

    private static void ThrowOnGraphQlErrors(JsonElement root)
    {
        if (root.OptionalPath("errors") is { ValueKind: JsonValueKind.Array } errors && errors.GetArrayLength() > 0)
        {
            throw new FormatException($"WorldRemit GraphQL error: {errors.GetRawText()}");
        }
    }
}

file static class StringExtensions
{
    public static string TrimStart(this string value, Func<char, bool> predicate)
    {
        var i = 0;
        while (i < value.Length && predicate(value[i]))
        {
            i++;
        }
        return value[i..];
    }
}
