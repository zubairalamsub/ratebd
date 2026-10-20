using System.Text.Json.Nodes;
using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

/// <summary>
/// UAE exchange houses that publish a rate but no fee (GCC, Orient, Lari), from responses captured on 2026-10-20
/// between 10:07:21 and 10:07:27 UTC with the engine's headers.
/// </summary>
public sealed class UaeRateTests
{
    // Shortly after the capture; Fixture.FetchedAt (2026-10-17) predates these responses.
    private static readonly DateTime FetchedAt = new(2026, 10, 20, 10, 8, 0, DateTimeKind.Utc);

    // ---------------------------------------------------------------- GCC Exchange

    [Fact]
    public void GccExchange_inverts_the_aed_per_bdt_rate_and_reads_uae_time()
    {
        var rate = GccExchangeParser.Parse(Fixture.Read("gccexchange-rates.json"), Fixture.AedBdt, FetchedAt);

        Assert.Equal(33.6447m, rate.Rate); // 1 / 0.029722404040773, shown by the widget as "1 AED = 33.6447 BDT"
        Assert.Equal("Not specified", rate.DeliveryMethod); // ServiceName "ALL"
        Assert.Equal(new DateTime(2026, 10, 20, 10, 0, 6, DateTimeKind.Utc), rate.PublishedAt); // 14:00:06 in Dubai
        Assert.Null(rate.FeeNote);
        Assert.Null(rate.Partner);
        Assert.False(rate.Stale);
    }

    [Fact]
    public void GccExchange_rejects_a_missing_timestamp_a_missing_row_and_a_failed_result()
    {
        var json = Fixture.Read("gccexchange-rates.json");
        const string stamp = "\"ExchangeRate\":\"0.029722404040773\",\"LastModifiedDate\":\"2026-10-20 14:00:06\"";
        Assert.Contains(stamp, json);

        var unreadable = Assert.Throws<FormatException>(() =>
            GccExchangeParser.Parse(json.Replace(stamp, "\"ExchangeRate\":\"0.029722404040773\",\"LastModifiedDate\":\"\""),
                Fixture.AedBdt, FetchedAt));
        Assert.Contains("no readable LastModifiedDate", unreadable.Message);

        var noRow = Assert.Throws<FormatException>(() =>
            GccExchangeParser.Parse(json.Replace("\"CurrencyCode\":\"BDT\"", "\"CurrencyCode\":\"XXX\""), Fixture.AedBdt, FetchedAt));
        Assert.Contains("no BDT row", noRow.Message);

        Assert.Throws<FormatException>(() =>
            GccExchangeParser.Parse(json.Replace("\"result\":\"1\"", "\"result\":\"0\""), Fixture.AedBdt, FetchedAt));
    }

    [Fact]
    public void GccExchange_rate_modified_days_ago_is_rejected()
    {
        var doc = JsonNode.Parse(Fixture.Read("gccexchange-rates.json"))!;
        var bdt = doc["result"]!["exchangerate"]!.AsArray().Single(r => (string?)r!["CurrencyCode"] == "BDT")!;
        bdt["LastModifiedDate"] = "2026-10-16 14:00:06";

        var ex = Assert.Throws<FormatException>(() => GccExchangeParser.Parse(doc.ToJsonString(), Fixture.AedBdt, FetchedAt));
        Assert.Contains("last updated", ex.Message);
    }

    [Fact]
    public async Task GccExchange_provider_sends_the_widget_request()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json(Fixture.Read("gccexchange-rates.json")));

        var rates = await new GccExchangeProvider(new HttpClient(handler), new FixedTime(FetchedAt))
            .GetRatesAsync(Fixture.AedBdt, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://gccexchange.com/exchangerate/getExchangeRate", request.RequestUri!.AbsoluteUri);
        Assert.Equal(["XMLHttpRequest"], request.Headers.GetValues("X-Requested-With"));
        Assert.Equal(new Uri("https://gccexchange.com/"), request.Headers.Referrer);
        Assert.Equal("", Assert.Single(handler.Bodies));
        var rate = Assert.Single(rates);
        Assert.Equal("GccExchange", rate.Provider);
        Assert.Equal(33.6447m, rate.Rate);
        Assert.Equal(FetchedAt, rate.FetchedAt);
    }

    // ---------------------------------------------------------------- Orient Exchange

    [Fact]
    public void OrientExchange_reads_the_transfer_rate_and_uae_time()
    {
        var rate = OrientExchangeParser.Parse(Fixture.Read("orientexchange-rates.json"), Fixture.AedBdt, FetchedAt);

        Assert.Equal(33.6501m, rate.Rate);
        Assert.Equal("Not specified", rate.DeliveryMethod);
        Assert.Equal(new DateTime(2026, 10, 20, 10, 7, 0, DateTimeKind.Utc), rate.PublishedAt); // 02.07 PM in Dubai
        Assert.Null(rate.FeeNote);
        Assert.False(rate.Stale);
    }

    [Fact]
    public void OrientExchange_rejects_an_unreadable_date_and_a_missing_rate()
    {
        var json = Fixture.Read("orientexchange-rates.json");

        // Every row and the top-level field carry the same stamp.
        var ex = Assert.Throws<FormatException>(() =>
            OrientExchangeParser.Parse(json.Replace("20 Oct 2026 02.07 PM", "N/A"), Fixture.AedBdt, FetchedAt));
        Assert.Contains("no readable update time", ex.Message);

        Assert.Throws<FormatException>(() =>
            OrientExchangeParser.Parse(json.Replace("\"Rate\":\"33.6501\"", "\"Rate\":null"), Fixture.AedBdt, FetchedAt));
        var noRow = Assert.Throws<FormatException>(() =>
            OrientExchangeParser.Parse(json.Replace("\"CurrencyCode\":\"BDT\"", "\"CurrencyCode\":\"XXX\""), Fixture.AedBdt, FetchedAt));
        Assert.Contains("no BDT row", noRow.Message);
    }

    [Fact]
    public async Task OrientExchange_provider_sends_the_currency_rates_request()
    {
        var handler = new FakeHandler(_ => FakeHandler.Html(Fixture.Read("orientexchange-rates.json")));

        var rates = await new OrientExchangeProvider(new HttpClient(handler), new FixedTime(FetchedAt))
            .GetRatesAsync(Fixture.AedBdt, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://www.orientexchange.com/Orient/GetExchangeRates", request.RequestUri!.AbsoluteUri);
        Assert.Equal(new Uri("https://www.orientexchange.com/Orient/CurrencyRates"), request.Headers.Referrer);
        Assert.False(request.Headers.Contains("X-Requested-With"));
        Assert.Equal("", Assert.Single(handler.Bodies));
        var rate = Assert.Single(rates);
        Assert.Equal("OrientExchange", rate.Provider);
        Assert.Equal(33.6501m, rate.Rate);
    }

    // ---------------------------------------------------------------- Lari Exchange
    // lariexchange-bdt.html is a TRIMMED excerpt of the 1,935,074-byte page: the original markup of the page top, the
    // first header carousel (LAST UPDATED and the BDT item) and the whole main content, with comments marking the cuts.
    // Line endings are normalised to LF, as the repository stores text files.

    [Fact]
    public void LariExchange_reads_the_transfer_rate_and_last_updated()
    {
        var rate = LariExchangeParser.Parse(Fixture.Read("lariexchange-bdt.html"), Fixture.AedBdt, FetchedAt);

        Assert.Equal(33.55m, rate.Rate);
        Assert.Equal("Not specified", rate.DeliveryMethod);
        Assert.Equal(new DateTime(2026, 10, 20, 9, 50, 0, DateTimeKind.Utc), rate.PublishedAt); // 20/10/2026 13:50 in Dubai
        Assert.Null(rate.FeeNote);
        Assert.False(rate.Stale);
    }

    [Fact]
    public void LariExchange_rejects_an_unreadable_date_a_missing_rate_and_other_rate_types()
    {
        var html = Fixture.Read("lariexchange-bdt.html");

        var noDate = Assert.Throws<FormatException>(() =>
            LariExchangeParser.Parse(html.Replace("20/10/2026 13:50", "--"), Fixture.AedBdt, FetchedAt));
        Assert.Contains("no readable LAST UPDATED", noDate.Message);

        var noRate = Assert.Throws<FormatException>(() =>
            LariExchangeParser.Parse(html.Replace("id=\"MainContent_txtExchangeRateDisplay\"", "id=\"other\""), Fixture.AedBdt,
                FetchedAt));
        Assert.Contains("no exchange-rate display", noRate.Message);

        Assert.Throws<FormatException>(() =>
            LariExchangeParser.Parse(html.Replace("font-weight:bold;\">33.55<", "font-weight:bold;\"><"), Fixture.AedBdt, FetchedAt));

        var cash = html
            .Replace("<option selected=\"selected\" value=\"tt_sell\">", "<option value=\"tt_sell\">")
            .Replace("<option value=\"cash_sell\">", "<option selected=\"selected\" value=\"cash_sell\">");
        var ex = Assert.Throws<FormatException>(() => LariExchangeParser.Parse(cash, Fixture.AedBdt, FetchedAt));
        Assert.Contains("cash_sell", ex.Message);
    }

    [Fact]
    public void LariExchange_page_dated_days_ago_is_rejected()
    {
        var html = Fixture.Read("lariexchange-bdt.html").Replace("20/10/2026 13:50", "16/10/2026 13:50");

        var ex = Assert.Throws<FormatException>(() => LariExchangeParser.Parse(html, Fixture.AedBdt, FetchedAt));
        Assert.Contains("last updated", ex.Message);
    }

    [Fact]
    public async Task LariExchange_provider_requests_the_bdt_page()
    {
        var handler = new FakeHandler(_ => FakeHandler.Html(Fixture.Read("lariexchange-bdt.html")));

        var rates = await new LariExchangeProvider(new HttpClient(handler), new FixedTime(FetchedAt))
            .GetRatesAsync(Fixture.AedBdt, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://www.lariexchange.com/ExchangeRates?Currency=BDT", request.RequestUri!.AbsoluteUri);
        Assert.Null(request.Headers.Referrer);
        Assert.False(request.Headers.Contains("X-Requested-With"));
        var rate = Assert.Single(rates);
        Assert.Equal("LariExchange", rate.Provider);
        Assert.Equal(33.55m, rate.Rate);
    }
}
