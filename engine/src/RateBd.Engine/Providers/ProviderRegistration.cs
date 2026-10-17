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
        AddProvider<WiseProvider>(services, configuration, "Wise");
        // Remitly answers bursts with 429 "try again in a couple of minutes": back off for longer.
        AddProvider<RemitlyProvider>(services, configuration, "Remitly", o => o.Retry.Delay = TimeSpan.FromSeconds(6));
        return services;
    }

    /// <summary>
    /// Registers a typed HttpClient for a provider with Polly-based retries and timeouts, and exposes it as an
    /// <see cref="IRateProvider"/> unless it is disabled via <c>Providers:{name}:Enabled = false</c>.
    /// </summary>
    private static void AddProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        IServiceCollection services,
        IConfiguration configuration,
        string name,
        Action<HttpStandardResilienceOptions>? tune = null)
        where TProvider : class, IRateProvider
    {
        var options = new ProviderOptions();
        configuration.GetSection($"Providers:{name}").Bind(options);
        if (!options.Enabled)
        {
            return;
        }

        services.AddHttpClient<TProvider>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUserAgent);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/html;q=0.9, */*;q=0.8");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,en;q=0.9");
            })
            .AddStandardResilienceHandler(o =>
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

        services.AddSingleton<IRateProvider>(sp => sp.GetRequiredService<TProvider>());
    }
}
