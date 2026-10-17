using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Taptap Send's public rates feed (used by taptapsend.com's calculator). It returns every corridor in one
/// response, so it is fetched once per run and shared across corridors. The three headers are required;
/// without them the API answers 400 BAD_HEADER.
/// </summary>
public sealed class TaptapSendProvider(HttpClient http, TimeProvider time) : IRateProvider
{
    internal const string FxRatesUrl = "https://api.taptapsend.com/api/fxRates";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private (string Json, DateTime FetchedAt)? _cached;

    public string Name => "TaptapSend";

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        var (json, fetchedAt) = await GetRatesAsync(ct);
        return TaptapSendParser.Parse(json, c, sendAmounts, fetchedAt);
    }

    private async Task<(string Json, DateTime FetchedAt)> GetRatesAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_cached is { } cached)
            {
                return cached;
            }
            using var request = new HttpRequestMessage(HttpMethod.Get, FxRatesUrl);
            request.Headers.Add("Appian-Version", "web/2022-05-03.0");
            request.Headers.Add("X-Device-Id", "web");
            request.Headers.Add("X-Device-Model", "web");
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var result = (await response.Content.ReadAsStringAsync(ct), time.GetUtcNow().UtcDateTime);
            _cached = result;
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }
}
