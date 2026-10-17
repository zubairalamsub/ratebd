using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RateBd.Engine.Json;

/// <summary>Helpers for reading provider responses with <see cref="JsonDocument"/> (AOT-safe, no reflection).</summary>
internal static class JsonExtensions
{
    public static JsonElement Path(this JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var name in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out current))
            {
                throw new FormatException($"Missing JSON property '{string.Join('.', path)}'.");
            }
        }
        return current;
    }

    public static JsonElement? OptionalPath(this JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var name in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out current)
                || current.ValueKind == JsonValueKind.Null)
            {
                return null;
            }
        }
        return current;
    }

    /// <summary>Reads a decimal that may be encoded as a JSON number or a string such as "163,926.20".</summary>
    public static decimal GetFlexibleDecimal(this JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.GetDecimal(),
        JsonValueKind.String => ParseDecimal(element.GetString()),
        _ => throw new FormatException($"Expected a number but found {element.ValueKind}."),
    };

    public static decimal ParseDecimal(string? text)
    {
        if (decimal.TryParse(text?.Replace(",", "", StringComparison.Ordinal).Trim(),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
        throw new FormatException($"'{text}' is not a decimal number.");
    }

    public static string? GetStringOrNull(this JsonElement? element) =>
        element is { ValueKind: JsonValueKind.String } e ? e.GetString() : null;

    /// <summary>Serialises a JSON body without reflection-based serialisation (Native AOT safe).</summary>
    public static StringContent ToContent(this JsonNode body) =>
        new(body.ToJsonString(), Encoding.UTF8, "application/json");
}
