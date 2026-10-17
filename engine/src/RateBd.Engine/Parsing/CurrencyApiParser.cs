using System.Text.Json;
using RateBd.Engine.Json;

namespace RateBd.Engine.Parsing;

/// <summary>Units of each currency per one USD, from the fawazahmed0/currency-api daily feed.</summary>
internal sealed record UsdCrossRates(string? Date, IReadOnlyDictionary<string, decimal> PerUsd)
{
    public decimal For(string currency) =>
        PerUsd.TryGetValue(currency.ToLowerInvariant(), out var rate) && rate > 0
            ? rate
            : throw new FormatException($"Cross-rate feed has no USD→{currency} rate");
}

/// <summary>Parses <c>.../v1/currencies/usd.json</c>: <c>{"date": "yyyy-MM-dd", "usd": {"gbp": 0.75, ...}}</c>.</summary>
internal static class CurrencyApiParser
{
    public static UsdCrossRates Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in root.Path("usd").EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDecimal(out var value))
            {
                rates[property.Name] = value;
            }
        }
        return new UsdCrossRates(root.OptionalPath("date").GetStringOrNull(), rates);
    }
}
