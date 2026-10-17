using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

/// <summary>Al Mulla and provider registration, from responses captured on 2026-10-17.</summary>
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
}
