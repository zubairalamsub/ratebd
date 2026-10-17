using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using RateBd.Engine.Json;
using RateBd.Engine.Models;
using RateBd.Engine.Parsing;

namespace RateBd.Engine.Providers;

/// <summary>
/// Skrill Money Transfer's public calculator (transfers.skrill.com/smt/calculator). The calculator first opens an
/// anonymous session with three plain GETs (authorize, session-register with its redirect through sso.skrill.com,
/// authorize again), which sets an access-token cookie and an <c>XSRF-TOKEN</c> cookie that the quote request echoes
/// as <c>x-xsrf-token</c>. No login is involved. The typed client's handler keeps cookies in <see cref="SkrillCookies"/>,
/// where the provider reads the token (a token set during a redirect never shows in the final response). Kuwait only: Skrill offers Saudi Arabia and Qatar in USD, not SAR or QAR, and hides Oman from
/// visitors (checked 2026-10-17).
/// </summary>
public sealed class SkrillProvider(HttpClient http, TimeProvider time, SkrillCookies cookies) : IRateProvider, ISendCountryScoped
{
    internal const string Origin = "https://transfers.skrill.com";
    internal const string AuthorizeUrl = Origin + "/api/login/authorize";
    internal const string SessionRegisterUrl = Origin + "/api/login/session-register?redirect_uri=" + Origin + "/smt/assets/auth/auth.html";
    internal const string PreviewUrl = Origin + "/api/transfers/v4/marketing/preview";
    internal const string CalculatorReferer = Origin + "/smt/calculator/marketing?theme=skrill-2020&appearance=flat";

    internal static readonly TimeSpan PauseBetweenRequests = TimeSpan.FromSeconds(3);

    // Skrill uses ISO 3166-1 alpha-3 country codes.
    private static readonly Dictionary<string, string> Alpha3 = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KW"] = "KWT",
        ["BD"] = "BGD",
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _xsrfToken;

    public string Name => "Skrill";

    public IReadOnlySet<string> SendCountries { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KW" };

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct)
    {
        if (!Alpha3.TryGetValue(c.SendCountry, out var from) || !Alpha3.TryGetValue(c.ReceiveCountry, out var to))
        {
            throw new ProviderNotSupportedException($"Skrill quotes are not collected for {c.SendCountry}→{c.ReceiveCountry}");
        }

        var quotes = new List<Quote>();
        foreach (var amount in sendAmounts)
        {
            if (quotes.Count > 0)
            {
                await Task.Delay(PauseBetweenRequests, time, ct);
            }
            var json = await PreviewAsync(c, from, to, amount, ct);
            // Bank deposit, like the other providers; Skrill prices bKash identically (same rate, no fee) but faster.
            quotes.Add(SkrillParser.Parse(json, c, amount, Labels.BankDeposit, time.GetUtcNow().UtcDateTime));
        }
        return quotes;
    }

    private async Task<string> PreviewAsync(Corridor c, string from, string to, decimal amount, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var token = await GetXsrfTokenAsync(refresh: attempt > 0, ct);
            var body = new JsonObject
            {
                ["direction"] = "S",
                ["senderCurrency"] = c.SendCurrency,
                ["senderCountry"] = from,
                ["recipientCurrency"] = c.ReceiveCurrency,
                ["recipientCountry"] = to,
                ["deliveryMethodType"] = "BANK",
                ["remittanceOperatorName"] = "<DEFAULT>",
                ["paymentOptionType"] = "DEBIT_CARD",
                ["senderAmount"] = amount.ToString("0.###", CultureInfo.InvariantCulture),
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, PreviewUrl) { Content = body.ToContent() };
            AddCalculatorHeaders(request);
            request.Headers.Add("x-xsrf-token", token);
            using var response = await http.SendAsync(request, ct);
            // 401 (codes 5279 no session / 5270 bad XSRF token) means the session lapsed: open a new one once.
            if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
            {
                continue;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
    }

    private async Task<string> GetXsrfTokenAsync(bool refresh, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_xsrfToken is { } cached && !refresh)
            {
                return cached;
            }
            string? issued = null;
            foreach (var url in new[] { AuthorizeUrl, SessionRegisterUrl, AuthorizeUrl })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddCalculatorHeaders(request);
                using var response = await http.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
                issued = XsrfToken(response) ?? issued;
            }
            // The jar holds the current cookie even when the server did not re-issue it on a refresh.
            _xsrfToken = cookies.Container.GetCookies(new Uri(Origin))["XSRF-TOKEN"]?.Value is { Length: > 0 } jar ? jar
                : issued ?? _xsrfToken ?? throw new HttpRequestException("Skrill did not issue an XSRF-TOKEN cookie");
            return _xsrfToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void AddCalculatorHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("adrum", "isAjax:true");
        request.Headers.Add("Origin", Origin);
        request.Headers.Referrer = new Uri(CalculatorReferer);
    }

    /// <summary>The <c>XSRF-TOKEN</c> value from the response's Set-Cookie headers, if any.</summary>
    internal static string? XsrfToken(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies
                .Select(cookie => cookie.Split(';', 2)[0].Split('=', 2))
                .Where(pair => pair.Length == 2 && pair[0].Trim() == "XSRF-TOKEN" && pair[1].Length > 0)
                .Select(pair => pair[1].Trim())
                .LastOrDefault()
            : null;
}
