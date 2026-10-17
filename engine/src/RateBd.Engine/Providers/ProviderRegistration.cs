using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using RateBd.Engine.Options;

namespace RateBd.Engine.Providers;

public static class ProviderRegistration
{
    internal const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";

    public static IServiceCollection AddRateProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MidMarketOptions>(configuration.GetSection(MidMarketOptions.Section));
        // Two cross-rate mirrors must both fit inside Engine:ProviderTimeoutSeconds, so each gets a shorter budget.
        AddProvider<MidMarketProvider>(services, configuration, "MidMarket", o =>
        {
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });
        AddProvider<WiseProvider>(services, configuration, "Wise");
        // Remitly's 429s cleared within seconds in testing; the provider itself retries once more with a fresh device ID.
        AddProvider<RemitlyProvider>(services, configuration, "Remitly");
        AddProvider<TaptapSendProvider>(services, configuration, "TaptapSend");
        AddProvider<WorldRemitProvider>(services, configuration, "WorldRemit");

        // Gulf providers (Saudi Arabia, Kuwait, Qatar, Oman), verified 2026-10-17.
        // Western Union's price depends on a cookie sent by hand, so its client must not manage cookies itself.
        AddProvider<WesternUnionProvider>(services, configuration, "WesternUnion",
            primaryHandler: _ => new HttpClientHandler { UseCookies = false });
        AddProvider<AlMullaProvider>(services, configuration, "AlMulla");
        // Skrill's XSRF token lives in a cookie; the provider reads it from a container it shares with its handler.
        services.AddSingleton<SkrillCookies>();
        AddProvider<SkrillProvider>(services, configuration, "Skrill",
            primaryHandler: sp => new HttpClientHandler { CookieContainer = sp.GetRequiredService<SkrillCookies>().Container });
        AddProvider<PaysendProvider>(services, configuration, "Paysend");

        // Exchange houses that publish a rate but no fee.
        AddPostedRateProvider<OoredooMoneyProvider>(services, configuration, "OoredooMoney");
        AddPostedRateProvider<GulfExchangeProvider>(services, configuration, "GulfExchange");
        AddPostedRateProvider<AlZamanProvider>(services, configuration, "AlZaman");
        AddPostedRateProvider<HabibQatarProvider>(services, configuration, "HabibQatar");
        AddPostedRateProvider<AlMirqabProvider>(services, configuration, "AlMirqab");
        AddPostedRateProvider<UnimoniProvider>(services, configuration, "Unimoni");
        AddPostedRateProvider<PKExchangeProvider>(services, configuration, "PKExchange");
        AddPostedRateProvider<JoyalukkasProvider>(services, configuration, "Joyalukkas");
        AddPostedRateProvider<AlAnsariProvider>(services, configuration, "AlAnsari");
        AddPostedRateProvider<AmanProvider>(services, configuration, "Aman");
        return services;
    }

    /// <summary>
    /// Registers a quoting provider and exposes it as an <see cref="IRateProvider"/> unless it is disabled via
    /// <c>Providers:{name}:Enabled = false</c>.
    /// </summary>
    private static void AddProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        IServiceCollection services,
        IConfiguration configuration,
        string name,
        Action<HttpStandardResilienceOptions>? tune = null,
        Func<IServiceProvider, HttpMessageHandler>? primaryHandler = null)
        where TProvider : class, IRateProvider
    {
        if (AddClient<TProvider>(services, configuration, name, tune, primaryHandler))
        {
            services.AddSingleton<IRateProvider>(sp => sp.GetRequiredService<TProvider>());
        }
    }

    /// <summary>Registers an exchange house that only posts rates, exposed as an <see cref="IPostedRateProvider"/>.</summary>
    private static void AddPostedRateProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        IServiceCollection services,
        IConfiguration configuration,
        string name)
        where TProvider : class, IPostedRateProvider
    {
        if (AddClient<TProvider>(services, configuration, name, tune: null, primaryHandler: null))
        {
            services.AddSingleton<IPostedRateProvider>(sp => sp.GetRequiredService<TProvider>());
        }
    }

    /// <summary>
    /// Registers a typed HttpClient for a provider with Polly-based retries and timeouts. Returns false when the
    /// provider is disabled via <c>Providers:{name}:Enabled = false</c> or left out of <c>Engine:OnlyProviders</c>.
    /// </summary>
    private static bool AddClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        IServiceCollection services,
        IConfiguration configuration,
        string name,
        Action<HttpStandardResilienceOptions>? tune,
        Func<IServiceProvider, HttpMessageHandler>? primaryHandler)
        where TProvider : class
    {
        var options = new ProviderOptions();
        configuration.GetSection($"Providers:{name}").Bind(options);
        var only = configuration.GetSection($"{EngineOptions.Section}:{nameof(EngineOptions.OnlyProviders)}").Get<string[]>() ?? [];
        if (!options.Enabled || (only.Length > 0 && !only.Contains(name, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        var client = services.AddHttpClient<TProvider>(http =>
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUserAgent);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/html;q=0.9, */*;q=0.8");
            http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,en;q=0.9");
        });
        if (primaryHandler is not null)
        {
            client.ConfigurePrimaryHttpMessageHandler(primaryHandler);
        }
        client.AddStandardResilienceHandler(o =>
        {
            o.Retry.MaxRetryAttempts = 3;
            o.Retry.Delay = TimeSpan.FromSeconds(2);
            o.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            o.Retry.UseJitter = true;
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            tune?.Invoke(o);
        });
        return true;
    }
}
