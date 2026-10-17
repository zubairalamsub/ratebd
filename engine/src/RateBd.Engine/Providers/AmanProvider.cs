using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Aman Exchange (Kuwait): the rate board on amankuwait.com, read from the portal API it calls. The board is dated;
/// a rate older than <see cref="PostedRates.MaxAge"/> is rejected. No fee is published. Verified 2026-10-17.
/// </summary>
public sealed class AmanProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string RateUrl = "https://portal.amankuwait.com/amsweb/api/rate";

    public string Name => "Aman";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KW" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct) =>
        [AmanParser.Parse(await http.GetStringAsync(RateUrl, ct), c, time.GetUtcNow().UtcDateTime)];
}
