using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

/// <summary>Western Union, Al Mulla, Skrill and Paysend, from responses captured on 2026-10-17.</summary>
public class GulfQuoteTests
{
    // ---------------------------------------------------------------- Western Union

    [Theory]
    [InlineData("westernunion-sar-bdt-1000.json", "sar", 1000, 32.8670397, 0, 32867.04)]
    [InlineData("westernunion-sar-bdt-2000.json", "sar", 2000, 32.8670397, 0, 65734.08)]
    [InlineData("westernunion-kwd-bdt-100.json", "kwd", 100, 400.8760545, 1.25, 39586.51)] // (100 - 1.25) × rate
    [InlineData("westernunion-qar-bdt-1000.json", "qar", 1000, 33.7945205, 0, 33794.52)]
    public void WesternUnion_takes_bank_deposit_and_normalises_the_fee(string fixture, string from, decimal amount, decimal rate,
        decimal fee, decimal receive)
    {
        var corridor = from switch { "sar" => Fixture.SarBdt, "kwd" => Fixture.KwdBdt, _ => Fixture.QarBdt };

        var quote = WesternUnionParser.Parse(Fixture.Read(fixture), corridor, amount, Fixture.FetchedAt);

        Assert.Equal("WesternUnion", quote.Provider);
        Assert.Equal(rate, quote.Rate);
        Assert.Equal(fee, quote.Fee);
        Assert.Equal(receive, quote.ReceiveAmount);
        Assert.Equal("Bank deposit", quote.DeliveryMethod);
        Assert.Equal("Within 1 day", quote.Speed);
    }

    [Fact]
    public void WesternUnion_rejects_the_price_tier_served_without_the_device_cookie()
    {
        // Same request without wu_device_id: no segment, lower rates that westernunion.com never shows.
        var ex = Assert.Throws<FormatException>(() =>
            WesternUnionParser.Parse(Fixture.Read("westernunion-sar-bdt-1000-nocookie.json"), Fixture.SarBdt, 1000, Fixture.FetchedAt));

        Assert.Contains("segment", ex.Message);
    }

    [Fact]
    public async Task WesternUnion_provider_sends_one_random_device_id_and_the_page_body()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(Fixture.Read("westernunion-sar-bdt-1000.json")));
        var provider = new WesternUnionProvider(new HttpClient(handler), TimeProvider.System);

        await provider.GetQuotesAsync(Fixture.SarBdt, [1000m], CancellationToken.None);
        await provider.GetQuotesAsync(Fixture.SarBdt, [1000m], CancellationToken.None);

        var cookies = handler.Requests.Select(r => Assert.Single(r.Headers.GetValues("Cookie"))).ToList();
        Assert.All(cookies, c => Assert.Matches("^wu_device_id=[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", c));
        Assert.Single(cookies.Distinct());
        Assert.Equal("text/plain", handler.Requests[0].Content!.Headers.ContentType!.MediaType);

        using var body = JsonDocument.Parse(handler.Bodies[0]);
        Assert.Equal("ARA100016", body.RootElement.GetProperty("sender").GetProperty("client").GetString());
        Assert.Equal("1000.00", body.RootElement.GetProperty("sender").GetProperty("send_amount").GetString());
    }

    [Fact]
    public async Task WesternUnion_provider_reports_a_bot_page_as_a_failure()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("<html>Access Denied</html>"),
        });
        var provider = new WesternUnionProvider(new HttpClient(handler), TimeProvider.System);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GetQuotesAsync(Fixture.KwdBdt, [100m], CancellationToken.None));
        Assert.Contains("403", ex.Message);
    }

    [Fact]
    public void WesternUnion_is_only_collected_for_saudi_kuwait_and_qatar()
    {
        var provider = new WesternUnionProvider(new HttpClient(), TimeProvider.System);

        Assert.Equal(["KW", "QA", "SA"], provider.SendCountries.Order());
    }

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

    // ---------------------------------------------------------------- Skrill

    [Theory]
    [InlineData("skrill-kwd-bdt-100-bank.json", 100, 37670.14)]
    [InlineData("skrill-kwd-bdt-200-bank.json", 200, 75340.28)] // its 10 KWD first-transfer discount is ignored
    public void Skrill_quotes_bank_deposit(string fixture, decimal amount, decimal receive)
    {
        var quote = SkrillParser.Parse(Fixture.Read(fixture), Fixture.KwdBdt, amount, "Bank deposit", Fixture.FetchedAt);

        Assert.Equal(376.7014m, quote.Rate);
        Assert.Equal(0m, quote.Fee);
        Assert.Equal(receive, quote.ReceiveAmount);
        Assert.Equal("Within 1 day", quote.Speed);
    }

    [Fact]
    public void Skrill_reads_the_xsrf_token_from_set_cookie()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("Set-Cookie", "accessToken=x; Path=/api; Secure; HttpOnly");
        response.Headers.Add("Set-Cookie", "XSRF-TOKEN=5b0b5569-29a4-4b87-83de-b5ac59c74353; Path=/; Secure");

        Assert.Equal("5b0b5569-29a4-4b87-83de-b5ac59c74353", SkrillProvider.XsrfToken(response));
    }

    [Fact]
    public async Task Skrill_provider_opens_a_session_then_echoes_the_token()
    {
        var handler = new FakeHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                var ok = FakeHandler.Json("""{"status":"NOT_LOGGED_IN"}""");
                ok.Headers.Add("Set-Cookie", "XSRF-TOKEN=token-1; Path=/; Secure");
                return ok;
            }
            return FakeHandler.Json(Fixture.Read("skrill-kwd-bdt-100-bank.json"));
        });

        var quotes = await new SkrillProvider(new HttpClient(handler), TimeProvider.System, new SkrillCookies())
            .GetQuotesAsync(Fixture.KwdBdt, [100m], CancellationToken.None);

        Assert.Single(quotes);
        Assert.Equal(3, handler.Requests.Count(r => r.Method == HttpMethod.Get));
        var preview = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Post);
        Assert.Equal("token-1", Assert.Single(preview.Headers.GetValues("x-xsrf-token")));
    }

    [Fact]
    public async Task Skrill_provider_reopens_the_session_once_on_401()
    {
        var posts = 0;
        var handler = new FakeHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                var ok = FakeHandler.Json("{}");
                ok.Headers.Add("Set-Cookie", "XSRF-TOKEN=t; Path=/");
                return ok;
            }
            return ++posts == 1
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("""{"code":5279}""") }
                : FakeHandler.Json(Fixture.Read("skrill-kwd-bdt-100-bank.json"));
        });

        await new SkrillProvider(new HttpClient(handler), TimeProvider.System, new SkrillCookies())
            .GetQuotesAsync(Fixture.KwdBdt, [100m], CancellationToken.None);

        Assert.Equal(6, handler.Requests.Count(r => r.Method == HttpMethod.Get));
        Assert.Equal(2, posts);
    }

    // ---------------------------------------------------------------- Paysend

    [Fact]
    public void Paysend_applies_the_page_tariff_to_each_amount()
    {
        var quotes = PaysendParser.Parse(Fixture.Read("paysend-kuwait-bangladesh-excerpt.html"), Fixture.KwdBdt, [100m, 200m],
            Fixture.FetchedAt);

        // The page itself shows 39,503.42 for 100 KWD and 79,205.35 for 200 KWD.
        Assert.Equal([39503.42m, 79205.35m], quotes.Select(q => q.ReceiveAmount));
        Assert.All(quotes, q => Assert.Equal(0.5m, q.Fee));
        Assert.All(quotes, q => Assert.Equal(397.019303m, q.Rate));
        Assert.All(quotes, q => Assert.Equal("Bank card", q.DeliveryMethod));
        Assert.All(quotes, q => Assert.Equal("In a few minutes", q.Speed));
    }

    [Fact]
    public void Paysend_page_showing_another_corridor_is_a_failure_not_a_missing_route()
    {
        // Kuwait is a route Paysend serves, so a different calculator on its page means the page changed: fail and
        // carry the last quote forward rather than quietly marking the route "not offered".
        var html = Fixture.Read("paysend-kuwait-bangladesh-excerpt.html").Replace("\"sourceCountry\":\"KW\"", "\"sourceCountry\":\"GB\"");

        Assert.Throws<FormatException>(() => PaysendParser.Parse(html, Fixture.KwdBdt, [100m], Fixture.FetchedAt));
    }

    [Fact]
    public void Paysend_percentage_tariff_is_clamped()
    {
        using var doc = JsonDocument.Parse("""{"percent":1.5,"fix":0.25,"minAmount":1,"maxAmount":2}""");

        Assert.Equal(1m, PaysendParser.FeeFor(doc.RootElement, 10m)); // 0.25 + 0.15, raised to the minimum
        Assert.Equal(1.75m, PaysendParser.FeeFor(doc.RootElement, 100m));
        Assert.Equal(2m, PaysendParser.FeeFor(doc.RootElement, 1000m)); // capped
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
    public async Task Skrill_reads_the_token_from_the_cookie_jar_when_it_is_not_reissued()
    {
        // A refresh sends the existing cookie, so the server need not set it again.
        var cookies = new SkrillCookies();
        cookies.Container.Add(new Uri(SkrillProvider.Origin), new Cookie("XSRF-TOKEN", "from-jar", "/"));
        var handler = new FakeHandler(request => request.Method == HttpMethod.Get
            ? FakeHandler.Json("{}")
            : FakeHandler.Json(Fixture.Read("skrill-kwd-bdt-100-bank.json")));

        await new SkrillProvider(new HttpClient(handler), TimeProvider.System, cookies)
            .GetQuotesAsync(Fixture.KwdBdt, [100m], CancellationToken.None);

        var preview = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Post);
        Assert.Equal("from-jar", Assert.Single(preview.Headers.GetValues("x-xsrf-token")));
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
