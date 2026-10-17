using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>POST https://www.unimoni.om/crm/live/rate</c> (form <c>code=BDT&amp;amount=N</c>), the converter on unimoni.om.
/// <c>Response.transferAmount</c> is what N OMR buys at the remittance rate, before Unimoni's service fee (charged on
/// top, not published). <c>cashSellAmount</c> is the cash-note rate and is ignored. Errors arrive as HTTP 200 with
/// a string <c>statusCode</c> other than "200".
/// </summary>
internal static class UnimoniParser
{
    public static PostedRate Parse(string json, Corridor corridor, decimal amount, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var code = root.OptionalPath("statusCode").GetStringOrNull();
        if (code != "200")
        {
            throw new FormatException($"Unimoni returned {code ?? "(no status)"}: {root.OptionalPath("statusMessage").GetStringOrNull()}");
        }
        var response = root.Path("Response");
        if (response.Path("currencyCode").GetString() != corridor.ReceiveCurrency)
        {
            throw new FormatException($"Unimoni answered for {response.Path("currencyCode").GetString()}, not {corridor.ReceiveCurrency}");
        }
        var rate = Math.Round(response.Path("transferAmount").GetFlexibleDecimal() / amount, 4, MidpointRounding.AwayFromZero);
        // The converter has no payout-method choice; the site shows the same figure for bank, cash and wallet.
        return PostedRates.Create("Unimoni", corridor, rate, Labels.NotSpecified, fetchedAt);
    }
}
