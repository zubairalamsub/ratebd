using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Al Mirqab Exchange (Qatar): the public "View Rates" table on almirqabexchange.com. No fee is published.
/// Verified 2026-10-17.
/// </summary>
public sealed class AlMirqabProvider(HttpClient http, TimeProvider time) : IPostedRateProvider
{
    internal const string RatesUrl = "https://almirqabexchange.com/view_rates.jsp";

    public string Name => "AlMirqab";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "QA" };

    public async Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct)
    {
        // Served as ISO-8859-1, which ReadAsStringAsync honours from the Content-Type header.
        using var response = await http.GetAsync(RatesUrl, ct);
        response.EnsureSuccessStatusCode();
        return [AlMirqabParser.Parse(await response.Content.ReadAsStringAsync(ct), c, time.GetUtcNow().UtcDateTime)];
    }
}
