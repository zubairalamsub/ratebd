using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

/// <summary>Al Mulla, from responses captured on 2026-10-17, and provider registration.</summary>
public class GulfQuoteTests
{
    // ---------------------------------------------------------------- Al Mulla

    [Theory]
    [InlineData("almulla-kwd-bdt-100.json", 100, 39586.51)]
    [InlineData("almulla-kwd-bdt-200.json", 200, 79674.12)]
    public void AlMulla_takes_the_best_route_with_its_fee_on_top(string fixture, decimal amount, decimal receive)
    {
        var quote = AlMullaParser.Parse(Fixture.Read(fixture), Fixture.KwdBdt, amount, Fixture.FetchedAt);

        Assert.Equal(400.8760545m, quote.Rate); // best of 11 routes; the next is 400.641026
        Assert.Equal(1.25m, quote.Fee);
        Assert.Equal(receive, quote.ReceiveAmount);
        Assert.Equal("Bank deposit", quote.DeliveryMethod);
        Assert.Equal("Within 2 days", quote.Speed); // processTimeTotalInSeconds 129600 = 36 h
    }

    [Fact]
    public async Task AlMulla_provider_posts_the_quote_request_as_a_form_field()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(Fixture.Read("almulla-kwd-bdt-100.json")));

        await new AlMullaProvider(new HttpClient(handler), TimeProvider.System)
            .GetQuotesAsync(Fixture.KwdBdt, [100m], CancellationToken.None);

        var body = Uri.UnescapeDataString(Assert.Single(handler.Bodies).Replace('+', ' '));
        Assert.Equal("""body={"foreignCountryId":97,"foreignCurrencyId":7,"quoteName":"BDT","localAmount":100}""", body);
    }

    // ---------------------------------------------------------------- Review follow-ups

    [Fact]
    public void AlMulla_ignores_routes_that_take_the_fee_from_the_recipient()
    {
        var doc = JsonNode.Parse(Fixture.Read("almulla-kwd-bdt-100.json"))!;
        var groups = doc["results"]![0]!["dynamicRoutingPricingList"]!.AsArray();
        var route = groups[0]!["NO_BENE_DEDUCT"]![0]!.DeepClone();
        route["exRateBreakup"]!["domXRate"] = 450m;
        groups[1]!["BENE_DEDUCT"]!.AsArray().Add(route);

        var quote = AlMullaParser.Parse(doc.ToJsonString(), Fixture.KwdBdt, 100, Fixture.FetchedAt);

        Assert.Equal(400.8760545m, quote.Rate);
    }

    [Fact]
    public void OnlyProviders_limits_registration_for_offline_runs()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Engine:OnlyProviders:0"] = "MidMarket" })
            .Build();
        var services = new ServiceCollection().AddSingleton(TimeProvider.System).AddLogging();
        services.AddRateProviders(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Equal(["MidMarket"], provider.GetServices<IRateProvider>().Select(p => p.Name));
        Assert.Empty(provider.GetServices<IPostedRateProvider>());
    }

    [Fact]
    public void Engine_clients_never_store_or_send_back_cookies()
    {
        // Some hosts set cookies on every response (Lari's firewall, Ahalia's Cloudflare); a retry must not echo them.
        var services = new ServiceCollection().AddSingleton(TimeProvider.System).AddLogging();
        services.AddRateProviders(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpMessageHandlerFactory>();
        var clients = provider.GetServices<IRateProvider>().Select(p => p.GetType().Name)
            .Concat(provider.GetServices<IPostedRateProvider>().Select(p => p.GetType().Name))
            .ToList();

        Assert.Contains("LariExchangeProvider", clients);
        Assert.Contains("AhaliaExchangeProvider", clients);
        Assert.All(clients, name =>
        {
            var handler = factory.CreateHandler(name);
            while (handler is DelegatingHandler delegating)
            {
                handler = delegating.InnerHandler!;
            }
            var useCookies = handler switch
            {
                HttpClientHandler h => h.UseCookies,
                SocketsHttpHandler s => s.UseCookies,
                _ => throw new InvalidOperationException($"{name} has an unexpected primary handler {handler.GetType()}"),
            };
            Assert.False(useCookies, name);
        });
    }

    [Fact]
    public async Task Providers_added_on_2026_10_20_register_and_answer_through_the_engine_clients()
    {
        // Each host answers with the response captured from it on 2026-10-20 (see UaeRateTests, HomepageRateTests and
        // UsaRateTests).
        var captured = new Dictionary<string, string>
        {
            ["www.baexpress.net"] = "baexpress-usd-bdt-500.json",
            ["gccexchange.com"] = "gccexchange-rates.json",
            ["www.orientexchange.com"] = "orientexchange-rates.json",
            ["www.lariexchange.com"] = "lariexchange-bdt.html",
            ["hadiexchange.ae"] = "hadiexpress-home.html",
            ["ahaliaexchange.com"] = "ahaliaexchange-home.html",
            ["www.sonaliexchange.com"] = "sonaliexchange-rates.html",
            ["www.mustafa.com.sg"] = "mustafa-home.html",
        };
        string[] names = ["BAExpress", "GccExchange", "OrientExchange", "LariExchange", "HadiExpress", "AhaliaExchange",
            "SonaliExchange", "Mustafa"];
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(names.Select((name, i) => KeyValuePair.Create($"Engine:OnlyProviders:{i}", (string?)name)))
            .Build();
        var handler = new FakeHandler(request => FakeHandler.Html(Fixture.Read(captured[request.RequestUri!.Host])));
        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(new FixedTime(new DateTime(2026, 10, 20, 10, 10, 0, DateTimeKind.Utc)))
            .AddLogging();
        services.AddRateProviders(configuration);
        // Every typed client keeps the engine's default headers and resilience pipeline but answers offline.
        services.ConfigureHttpClientDefaults(http => http.ConfigurePrimaryHttpMessageHandler(() => handler));
        using var provider = services.BuildServiceProvider();

        var quoting = Assert.Single(provider.GetServices<IRateProvider>());
        Assert.Equal("BAExpress", quoting.Name);
        var posted = provider.GetServices<IPostedRateProvider>().ToList();
        Assert.Equal(names[1..], posted.Select(p => p.Name));

        var quote = Assert.Single(await quoting.GetQuotesAsync(Fixture.UsdBdt, [500m], CancellationToken.None));
        Assert.Equal(123.3m, quote.Rate);
        var rates = new List<decimal>();
        foreach (var p in posted)
        {
            var corridor = p.SendCountries.Single() switch
            {
                "AE" => Fixture.AedBdt,
                "US" => Fixture.UsdBdt,
                "SG" => Fixture.SgdBdt,
                var country => throw new InvalidOperationException($"No test corridor for {country}"),
            };
            rates.AddRange((await p.GetRatesAsync(corridor, CancellationToken.None)).Select(r => r.Rate));
        }
        Assert.Equal([33.6447m, 33.6501m, 33.55m, 33.30m, 33.38m, 123.4m, 96.5065m], rates);
        Assert.Equal(8, handler.Requests.Count);
        Assert.All(handler.Requests, r =>
            Assert.Equal(ProviderRegistration.BrowserUserAgent, string.Join(' ', r.Headers.GetValues("User-Agent"))));
    }
}
