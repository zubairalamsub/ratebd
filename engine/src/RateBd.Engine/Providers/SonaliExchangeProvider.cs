using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Sonali Exchange Co. Inc. (Sonali Bank's US remittance company): the "Exchange Rates and Tariff" page on
/// sonaliexchange.com, which shows a single USD→BDT conversion rate. The page is undated and shows no fee or payout
/// method. Verified 2026-10-20.
/// </summary>
public sealed class SonaliExchangeProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string RatesUrl = "https://www.sonaliexchange.com/rates.asp";

    public string Name => "SonaliExchange";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "US" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        if (c.SendCurrency != "USD" || c.ReceiveCurrency != "BDT")
        {
            throw new ProviderNotSupportedException("Sonali Exchange is only collected for USD→BDT");
        }
        var html = await http.GetStringAsync(RatesUrl, ct);
        return [SonaliExchangeParser.Parse(html, c, time.GetUtcNow().UtcDateTime)];
    }
}
