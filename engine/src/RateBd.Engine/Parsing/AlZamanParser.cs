using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary><c>GET https://alzamanexchange.com/api/getRate.php?cid=6</c> (cid 6 = BDT): <c>{"success":true,"data":{"BRate_TT":"33.86"}}</c>.</summary>
internal static class AlZamanParser
{
    public static PostedRate Parse(string json, Corridor corridor, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.OptionalPath("success") is not { ValueKind: JsonValueKind.True })
        {
            throw new FormatException($"Al Zaman returned {root.GetRawText()}");
        }
        // TT: telegraphic transfer to a bank account.
        return PostedRates.Create("AlZaman", corridor, root.Path("data", "BRate_TT").GetFlexibleDecimal(), Labels.BankDeposit,
            fetchedAt);
    }
}
