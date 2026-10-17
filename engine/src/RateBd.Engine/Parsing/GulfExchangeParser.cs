using System.Text.Json;
using System.Text.RegularExpressions;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// gulfexchange.com.qa's currency converter: the home page issues a Laravel session cookie and CSRF token, then
/// <c>POST /en/convert-currency</c> returns <c>{"status":1,"currency_rate":{"rate":"&lt;receive&gt;","div_rate":"&lt;rate&gt;"}}</c>.
/// </summary>
internal static partial class GulfExchangeParser
{
    public static string ParseToken(string html)
    {
        var match = InlineTokenRegex().Match(html);
        if (!match.Success)
        {
            match = MetaTokenRegex().Match(html);
        }
        return match.Success
            ? match.Groups[1].Value
            : throw new FormatException("Gulf Exchange home page has no CSRF token");
    }

    public static PostedRate Parse(string json, Corridor corridor, DateTime fetchedAt)
    {
        // Without a valid session and token the endpoint answers 200 with an HTML page.
        if (!json.TrimStart().StartsWith('{'))
        {
            throw new FormatException("Gulf Exchange returned HTML instead of a rate (session or token rejected)");
        }
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.OptionalPath("status") is not { ValueKind: JsonValueKind.Number } status || status.GetInt32() != 1)
        {
            throw new FormatException($"Gulf Exchange converter returned {root.GetRawText()}");
        }
        var rate = root.Path("currency_rate", "div_rate").GetFlexibleDecimal();
        return PostedRates.Create("GulfExchange", corridor, rate, Labels.BankDeposit, fetchedAt);
    }

    [GeneratedRegex("""_token\s*[:=]\s*['"]([A-Za-z0-9]+)['"]""")]
    private static partial Regex InlineTokenRegex();

    [GeneratedRegex(@"<meta\s+name=""csrf-token""\s+content=""([^""]+)""")]
    private static partial Regex MetaTokenRegex();
}
