using System.Text.Json.Nodes;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// WorldRemit's public GraphQL API, the one worldremit.com's calculator calls. Introspection is disabled; the
/// query and mutation below are the ones the site's own JavaScript bundle sends. For each send amount the provider
/// prices every payout method and keeps the one that delivers the most. The UAE is not a WorldRemit send country,
/// so AE→BD returns no payout methods and is reported as not supported.
/// </summary>
public sealed class WorldRemitProvider(HttpClient http, TimeProvider time) : IRateProvider
{
    internal const string GraphQlUrl = "https://api.worldremit.com/graphql";

    private const string PayOutMethodsQuery =
        "query($s:CountryCode!,$r:CountryCode!,$c:CurrencyCode!){ payOutMethods(payOutMethodsInput:" +
        "{sendCountry:$s,receiveCountry:$r,receiveCurrency:$c}) { code payOutTimeEstimate correspondents { id name payOutTime } } }";

    private const string CalculationMutation =
        "mutation($amount:BigDecimal!,$type:CalculationType!,$s:CountryCode!,$sc:CurrencyCode!,$r:CountryCode!," +
        "$rc:CurrencyCode!,$pm:String,$cid:String){ createCalculation(calculationInput:{amount:$amount," +
        "send:{country:$s,currency:$sc},type:$type,receive:{country:$r,currency:$rc},payOutMethodCode:$pm," +
        "correspondentId:$cid}){ calculation{ id isFree informativeSummary{ fee{ value{amount currency} type } " +
        "discount{ value{amount currency} type } totalToPay{amount} } send{currency amount} receive{amount currency} " +
        "exchangeRate{value crossedOutValue} } errors{ __typename ... on GenericCalculationError{message genericType:type} " +
        "... on ValidationCalculationError{message type code description} } } }";

    public string Name => "WorldRemit";

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        var methodsJson = await PostAsync(PayOutMethodsQuery, new JsonObject
        {
            ["s"] = c.SendCountry,
            ["r"] = c.ReceiveCountry,
            ["c"] = c.ReceiveCurrency,
        }, ct);
        var methods = WorldRemitParser.ParsePayOutMethods(methodsJson);
        if (methods.Count == 0)
        {
            throw new ProviderNotSupportedException(
                $"WorldRemit lists no payout methods for {c.SendCountry}→{c.ReceiveCountry} ({c.SendCountry} is not a WorldRemit send country)");
        }

        var quotes = new List<Quote>();
        foreach (var amount in sendAmounts)
        {
            var priced = new List<Quote>();
            foreach (var method in methods)
            {
                var json = await PostAsync(CalculationMutation, new JsonObject
                {
                    ["amount"] = amount,
                    ["type"] = "SEND",
                    ["s"] = c.SendCountry,
                    ["sc"] = c.SendCurrency,
                    ["r"] = c.ReceiveCountry,
                    ["rc"] = c.ReceiveCurrency,
                    ["pm"] = method.Code,
                    ["cid"] = method.CorrespondentId,
                }, ct);
                priced.Add(WorldRemitParser.ParseCalculation(json, c, amount, method, time.GetUtcNow().UtcDateTime));
            }
            quotes.Add(priced.MaxBy(q => q.ReceiveAmount)!);
        }
        return quotes;
    }

    private async Task<string> PostAsync(string query, JsonObject variables, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlUrl)
        {
            Content = new JsonObject { ["query"] = query, ["variables"] = variables }.ToContent(),
        };
        request.Headers.Add("X-WR-Platform", "Web");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
