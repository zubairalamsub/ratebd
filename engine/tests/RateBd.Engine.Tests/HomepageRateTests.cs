using RateBd.Engine.Models;
using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

/// <summary>
/// Exchange houses whose rate is embedded in their home page, from pages captured on 2026-10-20 at 10:07 UTC:
/// <list type="bullet">
/// <item><c>hadiexpress-home.html</c>: https://hadiexchange.ae/ (549,640 bytes) trimmed to an unmodified excerpt, lines
/// 427–509, the "Money Transfer Rate" converter and its note.</item>
/// <item><c>ahaliaexchange-home.html</c>: https://ahaliaexchange.com/ (440,084 bytes) trimmed to an unmodified excerpt,
/// lines 1184–2301, from the "Bank Transfer Rate" board to the end of the calculator script, including <c>cc_data</c>.</item>
/// <item><c>mustafa-home.html</c>: https://www.mustafa.com.sg/ in full (20,709 bytes; line endings normalised to LF).</item>
/// </list>
/// None of the three publishes an update time, so none sets <see cref="PostedRate.PublishedAt"/>.
/// </summary>
public sealed class HomepageRateTests
{
    // Shortly after the captures (10:07:14–10:07:15 UTC).
    private static readonly DateTime FetchedAt = new(2026, 10, 20, 10, 10, 0, DateTimeKind.Utc);

    // ---------------------------------------------------------------- Hadi Express (UAE)

    [Fact]
    public void HadiExpress_reads_the_bdt_data_rate()
    {
        var rate = HadiExpressParser.Parse(Fixture.Read("hadiexpress-home.html"), Fixture.AedBdt, FetchedAt);

        Assert.Equal("HadiExpress", rate.Provider);
        Assert.Equal(33.30m, rate.Rate); // data-type="BDT" data-rate="33.30"; INR (25.75) is the converter's default
        Assert.Equal("Not specified", rate.DeliveryMethod);
        Assert.Null(rate.PublishedAt);
        Assert.Null(rate.FeeNote);
        Assert.Equal(FetchedAt, rate.FetchedAt);
        Assert.False(rate.Stale);
    }

    [Theory]
    [InlineData("data-type=\"BDT\"", "data-type=\"XYZ\"")]
    [InlineData("data-type=\"BDT\" data-rate=\"33.30\"", "data-type=\"BDT\" data-rate=\"\"")]
    [InlineData("data-type=\"BDT\" data-rate=\"33.30\"", "data-type=\"BDT\" data-rate=\"0.00\"")]
    public void HadiExpress_without_a_usable_bdt_rate_fails(string find, string replace)
    {
        var html = Fixture.Read("hadiexpress-home.html");
        Assert.Contains(find, html);

        Assert.Throws<FormatException>(() => HadiExpressParser.Parse(html.Replace(find, replace), Fixture.AedBdt, FetchedAt));
    }

    [Fact]
    public async Task HadiExpress_provider_gets_the_home_page_with_no_extra_headers()
    {
        var handler = new FakeHandler(_ => FakeHandler.Html(Fixture.Read("hadiexpress-home.html")));

        var rates = await new HadiExpressProvider(new HttpClient(handler), new FixedTime(FetchedAt))
            .GetRatesAsync(Fixture.AedBdt, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://hadiexchange.ae/", request.RequestUri!.AbsoluteUri);
        Assert.Empty(request.Headers);
        Assert.Equal(33.30m, Assert.Single(rates).Rate);
        Assert.Equal(FetchedAt, rates[0].FetchedAt);
    }

    // ---------------------------------------------------------------- Ahalia Exchange (UAE)

    [Fact]
    public void AhaliaExchange_reads_cc_data_not_the_stale_table()
    {
        var html = Fixture.Read("ahaliaexchange-home.html");
        Assert.Contains("<span id=\"bdt\">33.32</span>", html); // the static figure the script overwrites

        var rate = AhaliaExchangeParser.Parse(html, Fixture.AedBdt, FetchedAt);

        Assert.Equal("AhaliaExchange", rate.Provider);
        Assert.Equal(33.38m, rate.Rate); // cc_data "bdt":"33.38"
        Assert.Equal("Bank deposit", rate.DeliveryMethod); // the board is headed "Bank Transfer Rate"
        Assert.Null(rate.PublishedAt);
        Assert.Null(rate.FeeNote);
        Assert.False(rate.Stale);
    }

    [Theory]
    [InlineData("var cc_data = {", "var other_data = {")] // script missing
    [InlineData("\"bdt\":\"33.38\",", "")] // no BDT entry
    [InlineData("\"bdt\":\"33.38\"", "\"bdt\":\"\"")] // empty value
    [InlineData("\"bdt\":\"33.38\"", "\"bdt\":33.38\"")] // broken JSON
    [InlineData("var cc_data = {", "var cc_data = [")] // not an object
    public void AhaliaExchange_without_a_usable_cc_data_rate_fails(string find, string replace)
    {
        var html = Fixture.Read("ahaliaexchange-home.html");
        Assert.Contains(find, html);

        Assert.Throws<FormatException>(() => AhaliaExchangeParser.Parse(html.Replace(find, replace), Fixture.AedBdt, FetchedAt));
    }

    [Fact]
    public async Task AhaliaExchange_provider_gets_the_home_page_with_no_extra_headers()
    {
        var handler = new FakeHandler(_ => FakeHandler.Html(Fixture.Read("ahaliaexchange-home.html")));

        var rates = await new AhaliaExchangeProvider(new HttpClient(handler), new FixedTime(FetchedAt))
            .GetRatesAsync(Fixture.AedBdt, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://ahaliaexchange.com/", request.RequestUri!.AbsoluteUri);
        Assert.Empty(request.Headers);
        Assert.Equal(33.38m, Assert.Single(rates).Rate);
    }

    // ---------------------------------------------------------------- Mustafa (Singapore)

    [Fact]
    public void Mustafa_reads_todays_draft_rate_and_ignores_the_server_clock()
    {
        var html = Fixture.Read("mustafa-home.html");
        Assert.Contains("Last Updated On:20-10-2026", html); // the time the page was served, not a publish time

        var rate = MustafaParser.Parse(html, Fixture.SgdBdt, "Bangladesh Taka", FetchedAt);

        Assert.Equal("Mustafa", rate.Provider);
        Assert.Equal(96.5065m, rate.Rate);
        Assert.Equal("Not specified", rate.DeliveryMethod);
        Assert.Null(rate.PublishedAt);
        Assert.Null(rate.FeeNote);
        Assert.False(rate.Stale);
    }

    [Theory]
    [InlineData(">Draft Rates</h3>", ">Gold Rates</h3>")]
    [InlineData("Bangladesh Taka </h3>", "Indian Rupee </h3>")]
    [InlineData(">Todays Rate</td>", ">Yesterdays Rate</td>")]
    [InlineData(">96.5065</td>", ">-</td>")]
    public void Mustafa_without_a_usable_draft_rate_fails(string find, string replace)
    {
        var html = Fixture.Read("mustafa-home.html");
        Assert.Contains(find, html);

        Assert.Throws<FormatException>(() =>
            MustafaParser.Parse(html.Replace(find, replace), Fixture.SgdBdt, "Bangladesh Taka", FetchedAt));
    }

    [Fact]
    public async Task Mustafa_provider_gets_the_home_page_with_no_extra_headers()
    {
        var handler = new FakeHandler(_ => FakeHandler.Html(Fixture.Read("mustafa-home.html")));

        var rates = await new MustafaProvider(new HttpClient(handler), new FixedTime(FetchedAt))
            .GetRatesAsync(Fixture.SgdBdt, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://www.mustafa.com.sg/", request.RequestUri!.AbsoluteUri);
        Assert.Empty(request.Headers);
        Assert.Equal(96.5065m, Assert.Single(rates).Rate);
        Assert.Null(rates[0].PublishedAt);
    }

    // ---------------------------------------------------------------- Shared

    [Fact]
    public async Task Providers_skip_corridors_their_page_does_not_quote_without_a_request()
    {
        var handler = new FakeHandler(_ => throw new InvalidOperationException("no request expected"));
        var http = new HttpClient(handler);
        var aedToInr = Fixture.AedBdt with { Id = "aed-inr", ReceiveCurrency = "INR", ReceiveCountry = "IN" };

        await Assert.ThrowsAsync<ProviderNotSupportedException>(() =>
            new HadiExpressProvider(http, TimeProvider.System).GetRatesAsync(aedToInr, CancellationToken.None));
        await Assert.ThrowsAsync<ProviderNotSupportedException>(() =>
            new AhaliaExchangeProvider(http, TimeProvider.System).GetRatesAsync(Fixture.UsdBdt, CancellationToken.None));
        await Assert.ThrowsAsync<ProviderNotSupportedException>(() =>
            new MustafaProvider(http, TimeProvider.System).GetRatesAsync(Fixture.MyrBdt, CancellationToken.None));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void Providers_are_scoped_to_their_send_country()
    {
        var http = new HttpClient();

        Assert.Equal("AE", Assert.Single(new HadiExpressProvider(http, TimeProvider.System).SendCountries));
        Assert.Equal("AE", Assert.Single(new AhaliaExchangeProvider(http, TimeProvider.System).SendCountries));
        Assert.Equal("SG", Assert.Single(new MustafaProvider(http, TimeProvider.System).SendCountries));
    }
}
