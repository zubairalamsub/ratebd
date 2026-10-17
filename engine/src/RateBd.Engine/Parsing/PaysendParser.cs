using System.Text;
using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Parsing;

/// <summary>
/// Parses Paysend's server-rendered corridor page (<c>https://paysend.com/en/send-money/from-{country}-to-bangladesh</c>).
/// The page embeds its calculator state in <c>window.__INITIAL_STATE__=</c>, a JavaScript object literal: one rate,
/// and the fee tariff (<c>appliedTariffTo</c>: percent + fix, optionally clamped) the page applies to any amount.
/// </summary>
internal static class PaysendParser
{
    private const string StateMarker = "window.__INITIAL_STATE__=";
    private const string CalculatorKey = "\"calculator\":";

    public static IReadOnlyList<Quote> Parse(string html, Corridor corridor, decimal[] sendAmounts, DateTime fetchedAt)
    {
        var start = html.IndexOf(StateMarker, StringComparison.Ordinal);
        var key = start < 0 ? -1 : html.IndexOf(CalculatorKey, start, StringComparison.Ordinal);
        if (key < 0)
        {
            throw new FormatException("Paysend page has no calculator state");
        }

        using var doc = JsonDocument.Parse(ToJson(ExtractObject(html, key + CalculatorKey.Length)));
        var calculator = doc.RootElement;

        // A page for a corridor Paysend doesn't serve falls back to another corridor's calculator (the Saudi page shows
        // UK → China), so both ends are checked. Collected routes are ones Paysend serves, so a mismatch is a failure.
        var source = calculator.Path("sourceCountry").GetString();
        var target = calculator.Path("targetCurrency").GetString();
        if (source != corridor.SendCountry || target != corridor.ReceiveCurrency)
        {
            throw new FormatException(
                $"Paysend's {corridor.SendCountry}→{corridor.ReceiveCountry} page shows a {source}→{target} calculator instead");
        }

        var transaction = calculator.Path("transaction");
        // conversionRate carries ~17 significant digits ("397.01930300026731"); six decimals are plenty.
        var rate = Math.Round(JsonExtensions.ParseDecimal(transaction.Path("conversionRate").GetString()), 6,
            MidpointRounding.AwayFromZero);
        var tariff = transaction.Path("appliedTariffTo").EnumerateArray().FirstOrDefault();
        if (tariff.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Paysend calculator has no fee tariff");
        }
        var speed = transaction.OptionalPath("captions", "processingTime").GetStringOrNull() is { } caption
            ? caption.Replace("Should arrive:", "", StringComparison.OrdinalIgnoreCase).Trim()
            : Labels.NotSpecified;

        // The fee is deducted from the amount sent (the sender pays exactly the amount), matching our normalisation.
        // Payouts go to a bank card (UnionPay, Visa or Mastercard).
        return sendAmounts
            .Select(amount =>
            {
                var fee = FeeFor(tariff, amount);
                return new Quote("Paysend", corridor.Id, corridor.SendCurrency, amount, rate, fee,
                    QuoteMath.ReceiveFor(amount, fee, rate), Labels.BankCard, speed, fetchedAt, Stale: false);
            })
            .ToList();
    }

    /// <summary>fee = fix + percent% × amount, clamped to minAmount/maxAmount when set (as the page computes it).</summary>
    internal static decimal FeeFor(JsonElement tariff, decimal amount)
    {
        var fee = tariff.Path("fix").GetFlexibleDecimal()
                  + Math.Round(amount * tariff.Path("percent").GetFlexibleDecimal() / 100m, 3, MidpointRounding.AwayFromZero);
        if (tariff.OptionalPath("minAmount") is { } min)
        {
            fee = Math.Max(fee, min.GetFlexibleDecimal());
        }
        if (tariff.OptionalPath("maxAmount") is { } max)
        {
            fee = Math.Min(fee, max.GetFlexibleDecimal());
        }
        return fee;
    }

    /// <summary>Returns the balanced <c>{...}</c> starting at <paramref name="start"/>, skipping braces inside strings.</summary>
    private static string ExtractObject(string text, int start)
    {
        if (start >= text.Length || text[start] != '{')
        {
            throw new FormatException("Paysend calculator state is not an object");
        }
        var depth = 0;
        var inString = false;
        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (c == '\\')
                {
                    i++;
                }
                else if (c == '"')
                {
                    inString = false;
                }
                continue;
            }
            switch (c)
            {
                case '"':
                    inString = true;
                    break;
                case '{':
                    depth++;
                    break;
                case '}' when --depth == 0:
                    return text[start..(i + 1)];
            }
        }
        throw new FormatException("Paysend calculator state is truncated");
    }

    /// <summary>JavaScript allows escapes such as <c>\(</c> that JSON rejects; drop the backslash from those.</summary>
    private static string ToJson(string literal)
    {
        var sb = new StringBuilder(literal.Length);
        for (var i = 0; i < literal.Length; i++)
        {
            if (literal[i] == '\\' && i + 1 < literal.Length)
            {
                var next = literal[i + 1];
                if (next is not ('"' or '\\' or '/' or 'b' or 'f' or 'n' or 'r' or 't' or 'u'))
                {
                    continue;
                }
                sb.Append('\\').Append(next);
                i++;
                continue;
            }
            sb.Append(literal[i]);
        }
        return sb.ToString();
    }
}
