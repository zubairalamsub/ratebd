using System.Text.Json;
using RateBd.Engine.Json;
using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>
/// <c>GET https://hqie.com.qa/Account/ConvertCurr</c>, the converter on hqie.com.qa. Besides a large registration
/// view-model it returns <c>CurrentRate</c> (BDT per QAR, what the converter multiplies by) and its inverse
/// <c>CRMT_TELLRATE</c>. The converter has no payout-method choice.
/// </summary>
internal static class HabibQatarParser
{
    public static PostedRate Parse(string json, Corridor corridor, DateTime fetchedAt)
    {
        using var doc = JsonDocument.Parse(json);
        var rate = doc.RootElement.Path("CurrentRate").GetFlexibleDecimal();
        return PostedRates.Create("HabibQatar", corridor, rate, Labels.NotSpecified, fetchedAt);
    }
}
