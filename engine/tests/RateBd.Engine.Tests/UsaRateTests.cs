using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

/// <summary>BA Express USA and Sonali Exchange, from responses captured on 2026-10-20 between 10:07 and 10:09 UTC.</summary>
public sealed class UsaRateTests
{
    private static readonly DateTime FetchedAt = new(2026, 10, 20, 10, 10, 0, DateTimeKind.Utc);

    /// <summary>Returns a fixed clock and records each delay, which then completes at once.</summary>
    private sealed class TestTime(DateTime utcNow) : TimeProvider
    {
        public List<TimeSpan> Delays { get; } = [];

        public override DateTimeOffset GetUtcNow() => new(utcNow);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Delays.Add(dueTime);
            return base.CreateTimer(callback, state, TimeSpan.Zero, period);
        }
    }

    // ---------------------------------------------------------------- BA Express USA

    [Theory]
    [InlineData("baexpress-usd-bdt-500.json", 500, 2.00, 61403.40)]
    [InlineData("baexpress-usd-bdt-1000.json", 1000, 4.00, 122806.80)]
    public void BAExpress_quotes_the_rate_with_the_fee_on_top(string fixture, decimal amount, decimal fee, decimal receive)
    {
        var quote = BAExpressParser.Parse(Fixture.Read(fixture), Fixture.UsdBdt, amount, FetchedAt);

        Assert.Equal("BAExpress", quote.Provider);
        Assert.Equal("usd-bdt", quote.Corridor);
        Assert.Equal("USD", quote.SendCurrency);
        Assert.Equal(amount, quote.SendAmount);
        Assert.Equal(123.3m, quote.Rate); // "ExRate" : "123.300000"
        Assert.Equal(fee, quote.Fee); // ServiceFee, HandlingFee "0.00"
        Assert.Equal(receive, quote.ReceiveAmount); // (amount - fee) × 123.3
        Assert.Equal("Not specified", quote.DeliveryMethod);
        Assert.Equal("Not specified", quote.Speed);
        Assert.Equal(FetchedAt, quote.FetchedAt);
        Assert.False(quote.Stale);
    }

    [Fact]
    public void BAExpress_fee_includes_the_handling_fee()
    {
        var json = Fixture.Read("baexpress-usd-bdt-500.json").Replace("HandlingFee\\\" : \\\"0.00", "HandlingFee\\\" : \\\"1.50");

        var quote = BAExpressParser.Parse(json, Fixture.UsdBdt, 500m, FetchedAt);

        Assert.Equal(3.50m, quote.Fee);
        Assert.Equal(61218.45m, quote.ReceiveAmount); // 496.50 × 123.3
    }

    [Fact]
    public void BAExpress_empty_table_is_rejected()
    {
        var ex = Assert.Throws<FormatException>(() =>
            BAExpressParser.Parse("""{"d":"{\"Table\" : []}"}""", Fixture.UsdBdt, 500m, FetchedAt));
        Assert.Contains("no rate", ex.Message);
    }

    [Fact]
    public void BAExpress_missing_rate_is_rejected()
    {
        var json = Fixture.Read("baexpress-usd-bdt-500.json").Replace("\\\"ExRate\\\"", "\\\"Rate\\\"");

        var ex = Assert.Throws<FormatException>(() => BAExpressParser.Parse(json, Fixture.UsdBdt, 500m, FetchedAt));
        Assert.Contains("ExRate", ex.Message);
    }

    [Fact]
    public void BAExpress_answer_without_the_inner_document_is_rejected()
    {
        Assert.Throws<FormatException>(() => BAExpressParser.Parse("""{"d":null}""", Fixture.UsdBdt, 500m, FetchedAt));
    }

    [Fact]
    public async Task BAExpress_provider_posts_each_amount_like_the_calculator_and_pauses_between()
    {
        var fixtures = new Queue<string>(["baexpress-usd-bdt-500.json", "baexpress-usd-bdt-1000.json"]);
        var handler = new FakeHandler(_ => FakeHandler.Json(Fixture.Read(fixtures.Dequeue())));
        var time = new TestTime(FetchedAt);

        var quotes = await new BAExpressProvider(new HttpClient(handler), time)
            .GetQuotesAsync(Fixture.UsdBdt, [500m, 1000m], CancellationToken.None);

        Assert.Equal(
        [
            """{"p_SendingCurrency":"USD","p_SendingAmount":"500.00","p_FxCurrency":"BDT"}""",
            """{"p_SendingCurrency":"USD","p_SendingAmount":"1000.00","p_FxCurrency":"BDT"}""",
        ], handler.Bodies);
        Assert.All(handler.Requests, r =>
        {
            Assert.Equal(HttpMethod.Post, r.Method);
            Assert.Equal("https://www.baexpress.net/WebServices/WebService.asmx/Load_ExchangeRate_And_Fee", r.RequestUri!.AbsoluteUri);
            Assert.Equal("application/json; charset=utf-8", r.Content!.Headers.ContentType!.ToString());
            Assert.Equal("https://www.baexpress.net/", r.Headers.Referrer!.AbsoluteUri);
            Assert.Equal("XMLHttpRequest", Assert.Single(r.Headers.GetValues("X-Requested-With")));
            Assert.Equal(["Referer", "X-Requested-With"], r.Headers.Select(h => h.Key).Order());
        });
        Assert.Equal([BAExpressProvider.PauseBetweenRequests], time.Delays);

        Assert.Equal([500m, 1000m], quotes.Select(q => q.SendAmount));
        Assert.Equal([2.00m, 4.00m], quotes.Select(q => q.Fee));
        Assert.Equal([61403.40m, 122806.80m], quotes.Select(q => q.ReceiveAmount));
        Assert.All(quotes, q => Assert.Equal(FetchedAt, q.FetchedAt));
    }

    [Fact]
    public async Task BAExpress_is_collected_only_for_usd_from_the_us()
    {
        var handler = new FakeHandler(_ => throw new InvalidOperationException("no request expected"));
        var provider = new BAExpressProvider(new HttpClient(handler), TimeProvider.System);

        await Assert.ThrowsAsync<ProviderNotSupportedException>(() =>
            provider.GetQuotesAsync(Fixture.AedBdt, [500m], CancellationToken.None));
        Assert.Equal(["US"], provider.SendCountries);
        Assert.Empty(handler.Requests);
    }

    // ---------------------------------------------------------------- Sonali Exchange

    [Fact]
    public void SonaliExchange_reads_the_calculator_rate()
    {
        var rate = SonaliExchangeParser.Parse(Fixture.Read("sonaliexchange-rates.html"), Fixture.UsdBdt, FetchedAt);

        Assert.Equal("SonaliExchange", rate.Provider);
        Assert.Equal("usd-bdt", rate.Corridor);
        Assert.Equal(123.4m, rate.Rate); // <input id="aspRate" … value="123.4">; "1 USD = 123.4 Taka" above it
        Assert.Equal("Not specified", rate.DeliveryMethod);
        Assert.Equal("Not specified", rate.Speed);
        Assert.Null(rate.FeeNote);
        Assert.Null(rate.Partner);
        Assert.Null(rate.PublishedAt); // the page is undated
        Assert.Equal(FetchedAt, rate.FetchedAt);
        Assert.False(rate.Stale);
    }

    [Fact]
    public void SonaliExchange_missing_rate_field_is_rejected()
    {
        var html = Fixture.Read("sonaliexchange-rates.html").Replace("id=\"aspRate\"", "id=\"rate\"");

        var ex = Assert.Throws<FormatException>(() => SonaliExchangeParser.Parse(html, Fixture.UsdBdt, FetchedAt));
        Assert.Contains("aspRate", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("N/A")]
    [InlineData("0")]
    public void SonaliExchange_unreadable_or_zero_rate_is_rejected(string value)
    {
        var html = Fixture.Read("sonaliexchange-rates.html").Replace("value=\"123.4\" size=\"6\"", $"value=\"{value}\" size=\"6\"");

        Assert.Throws<FormatException>(() => SonaliExchangeParser.Parse(html, Fixture.UsdBdt, FetchedAt));
    }

    [Fact]
    public async Task SonaliExchange_provider_gets_the_rates_page()
    {
        var handler = new FakeHandler(_ => FakeHandler.Html(Fixture.Read("sonaliexchange-rates.html")));
        var provider = new SonaliExchangeProvider(new HttpClient(handler), new FixedTime(FetchedAt));

        var rates = await provider.GetRatesAsync(Fixture.UsdBdt, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://www.sonaliexchange.com/rates.asp", request.RequestUri!.AbsoluteUri);
        Assert.Empty(request.Headers); // only the engine's default client headers, none added per request
        Assert.Null(request.Content);
        var rate = Assert.Single(rates);
        Assert.Equal(123.4m, rate.Rate);
        Assert.Equal(FetchedAt, rate.FetchedAt);
        Assert.Equal(["US"], provider.SendCountries);
    }

    [Fact]
    public async Task SonaliExchange_is_collected_only_for_usd()
    {
        var handler = new FakeHandler(_ => throw new InvalidOperationException("no request expected"));
        var provider = new SonaliExchangeProvider(new HttpClient(handler), TimeProvider.System);

        await Assert.ThrowsAsync<ProviderNotSupportedException>(() => provider.GetRatesAsync(Fixture.AedBdt, CancellationToken.None));
        Assert.Empty(handler.Requests);
    }
}
