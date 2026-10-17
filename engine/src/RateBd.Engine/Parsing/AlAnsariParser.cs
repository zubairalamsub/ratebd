using System.Text.Json;
using System.Text.RegularExpressions;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// alansariexchange.com.kw's converter: WordPress <c>admin-ajax.php?action=convert_action</c>, authorised by the
/// anonymous nonce in the page's <c>CC_Ajax_Object</c> (the page's other nonce, <c>BN_Ajax_Object</c>, is refused).
/// The answer is <c>{"amount":"39615.41","get_rate":"396.15413565","status_msg":"SUCCESS"}</c>; the site labels it
/// an indicative rate.
/// </summary>
internal static partial class AlAnsariParser
{
    public static string ParseNonce(string html)
    {
        var match = NonceRegex().Match(html);
        return match.Success ? match.Groups[1].Value : throw new FormatException("Al Ansari page has no CC_Ajax_Object nonce");
    }

    public static PostedRate Parse(string json, Corridor corridor, DateTime fetchedAt)
    {
        // A missing or expired nonce is answered with HTTP 403 and the body "-1".
        var trimmed = json.Trim();
        if (trimmed == "-1")
        {
            throw new FormatException("Al Ansari converter returned -1 (nonce rejected)");
        }
        if (!trimmed.StartsWith('{'))
        {
            // One short line: the message ends up in the data file, on the site and in a Markdown table.
            var excerpt = string.Join(' ', trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            throw new FormatException($"Al Ansari converter returned non-JSON: '{excerpt[..Math.Min(excerpt.Length, 80)]}'");
        }
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.OptionalPath("status_msg").GetStringOrNull() != "SUCCESS")
        {
            throw new FormatException($"Al Ansari converter returned status '{root.OptionalPath("status_msg").GetStringOrNull()}'");
        }
        // trtype=BT is a bank transfer (CP, cash pickup, is priced separately).
        return PostedRates.Create("AlAnsari", corridor,
            Math.Round(root.Path("get_rate").GetFlexibleDecimal(), 4, MidpointRounding.AwayFromZero), Labels.BankDeposit, fetchedAt);
    }

    [GeneratedRegex(@"CC_Ajax_Object\s*=\s*\{[^}]*""ajax_nonce""\s*:\s*""([0-9a-f]+)""")]
    private static partial Regex NonceRegex();
}
