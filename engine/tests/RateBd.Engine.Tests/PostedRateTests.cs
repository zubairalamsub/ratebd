using Microsoft.Extensions.Logging.Abstractions;
using RateBd.Engine.Models;
using RateBd.Engine.Options;
using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;
using RateBd.Engine.Storage;

namespace RateBd.Engine.Tests;

/// <summary>Exchange houses that publish a rate but no fee, from responses captured on 2026-10-17.</summary>
public sealed class PostedRateTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ratebd-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    // ---------------------------------------------------------------- Qatar

    [Fact]
    public void Ooredoo_lists_each_partners_best_rate()
    {
        var rates = OoredooMoneyParser.Parse(Fixture.Read("ooredoomoney-bgd-bdt.json"), Fixture.QarBdt, Fixture.FetchedAt);

        Assert.Equal(8, rates.Count);
        Assert.All(rates, r => Assert.Equal("OoredooMoney", r.Provider));
        Assert.All(rates, r => Assert.Equal(new DateTime(2026, 10, 17, 12, 14, 32, 728, DateTimeKind.Utc), r.PublishedAt));

        var alzaman = Assert.Single(rates, r => r.Partner == "Alzaman Exchange");
        Assert.Equal(33.900144m, alzaman.Rate);
        Assert.Equal("Bank deposit", alzaman.DeliveryMethod);
        Assert.Equal("Same Day", alzaman.Speed);
        Assert.Equal("Starting from QAR 20", alzaman.FeeNote);

        // Western Union's wallet rate (33.7993594) beats its bank rate (33.7940826) and cash rate here.
        var wu = Assert.Single(rates, r => r.Partner == "Western Union");
        Assert.Equal(33.799359m, wu.Rate);
        Assert.Equal("Mobile wallet", wu.DeliveryMethod);

        // bKash and Nagad tie for Thunes; the first listed is kept, with the agent named.
        var thunes = Assert.Single(rates, r => r.Partner == "Thunes");
        Assert.Equal("Mobile wallet (bKash)", thunes.DeliveryMethod);
        Assert.Equal(33.743127m, thunes.Rate);
    }

    [Fact]
    public void Ooredoo_error_status_is_a_failure()
    {
        Assert.Throws<FormatException>(() =>
            OoredooMoneyParser.Parse(Fixture.Read("ooredoomoney-error-99999.json"), Fixture.QarBdt, Fixture.FetchedAt));
    }

    [Fact]
    public void GulfExchange_reads_the_token_and_the_converter_rate()
    {
        Assert.Equal("RedactedSessionToken0123456789abcdefghij",
            GulfExchangeParser.ParseToken(Fixture.Read("gulfexchange-home-excerpt.html")));

        var rate = GulfExchangeParser.Parse(Fixture.Read("gulfexchange-qar-bdt-1000.json"), Fixture.QarBdt, Fixture.FetchedAt);

        Assert.Equal(33.820346m, rate.Rate);
        Assert.Equal("Bank deposit", rate.DeliveryMethod);
    }

    [Fact]
    public void GulfExchange_html_answer_means_the_session_was_rejected()
    {
        var ex = Assert.Throws<FormatException>(() =>
            GulfExchangeParser.Parse("<!DOCTYPE html><html>…</html>", Fixture.QarBdt, Fixture.FetchedAt));
        Assert.Contains("session", ex.Message);
    }

    [Fact]
    public async Task GulfExchange_provider_posts_the_token_from_the_home_page()
    {
        var handler = new FakeHandler(request => request.Method == HttpMethod.Get
            ? new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(Fixture.Read("gulfexchange-home-excerpt.html")) }
            : FakeHandler.Json(Fixture.Read("gulfexchange-qar-bdt-1000.json")));

        await new GulfExchangeProvider(new HttpClient(handler), TimeProvider.System).GetRatesAsync(Fixture.QarBdt, CancellationToken.None);

        Assert.Contains("_token=RedactedSessionToken0123456789abcdefghij", handler.Bodies[1]);
        Assert.Contains("payment_method=bank_transfer", handler.Bodies[1]);
    }

    [Theory]
    [InlineData("alzaman", 33.86, "Bank deposit")]
    [InlineData("habibqatar", 33.90, "Not specified")]
    [InlineData("almirqab", 33.88, "Bank deposit")]
    public void Qatar_rate_boards(string source, decimal rate, string delivery)
    {
        var posted = source switch
        {
            "alzaman" => AlZamanParser.Parse(Fixture.Read("alzaman-bdt.json"), Fixture.QarBdt, Fixture.FetchedAt),
            "habibqatar" => HabibQatarParser.Parse(Fixture.Read("habibqatar-qar-bdt-1000.json"), Fixture.QarBdt, Fixture.FetchedAt),
            _ => AlMirqabParser.Parse(Fixture.Read("almirqab-view-rates.html"), Fixture.QarBdt, Fixture.FetchedAt),
        };

        Assert.Equal(rate, posted.Rate);
        Assert.Equal(delivery, posted.DeliveryMethod);
        Assert.Null(posted.FeeNote);
        Assert.False(posted.Stale);
    }

    // ---------------------------------------------------------------- Oman

    [Fact]
    public void Unimoni_derives_the_rate_from_the_converted_amount()
    {
        var rate = UnimoniParser.Parse(Fixture.Read("unimoni-bdt-1000.json"), Fixture.OmrBdt, 1000m, Fixture.FetchedAt);

        Assert.Equal(319.9995m, rate.Rate); // 319,999.49 BDT for 1,000 OMR
    }

    [Fact]
    public void Unimoni_error_arrives_as_http_200_with_a_status_code()
    {
        var ex = Assert.Throws<FormatException>(() =>
            UnimoniParser.Parse(Fixture.Read("unimoni-error-402.json"), Fixture.OmrBdt, 100m, Fixture.FetchedAt));
        Assert.Contains("402", ex.Message);
    }

    [Fact]
    public void PKExchange_reads_the_tt_rate_and_the_page_date()
    {
        var rate = PKExchangeParser.Parse(Fixture.Read("pkexchange-rates.html"), Fixture.OmrBdt, "BANGLADESH", Fixture.FetchedAt);

        Assert.Equal(320.00m, rate.Rate);
        Assert.Equal("Bank deposit", rate.DeliveryMethod);
        Assert.Equal(new DateTime(2026, 10, 16, 20, 0, 0, DateTimeKind.Utc), rate.PublishedAt); // 17/10/2026 in Muscat
    }

    [Fact]
    public void PKExchange_page_dated_days_ago_is_rejected()
    {
        var html = Fixture.Read("pkexchange-rates.html").Replace("17/10/2026", "10/10/2026");

        var ex = Assert.Throws<FormatException>(() => PKExchangeParser.Parse(html, Fixture.OmrBdt, "BANGLADESH", Fixture.FetchedAt));
        Assert.Contains("last updated", ex.Message);
    }

    [Theory]
    [InlineData("joyalukkas-region1-bdt.json", "kwd", 400.8765)] // 1 / 0.0024945340
    [InlineData("joyalukkas-region3-bdt.json", "omr", 320.0216)] // 1 / 0.0031247890
    public void Joyalukkas_inverts_the_tt_rate(string fixture, string from, decimal expected)
    {
        var corridor = from == "kwd" ? Fixture.KwdBdt : Fixture.OmrBdt;

        var rate = JoyalukkasParser.Parse(Fixture.Read(fixture), corridor, Fixture.FetchedAt);

        Assert.Equal(expected, rate.Rate);
        Assert.Equal("Bank deposit", rate.DeliveryMethod);
    }

    // ---------------------------------------------------------------- Kuwait

    [Fact]
    public void AlAnsari_uses_the_converter_nonce_not_the_first_one()
    {
        Assert.Equal("76a2864716", AlAnsariParser.ParseNonce(Fixture.Read("alansari-home-excerpt.html")));
    }

    [Fact]
    public void AlAnsari_bank_transfer_rate()
    {
        var rate = AlAnsariParser.Parse(Fixture.Read("alansari-bt-1.json"), Fixture.KwdBdt, Fixture.FetchedAt);

        Assert.Equal(396.1541m, rate.Rate);
        Assert.Equal("Bank deposit", rate.DeliveryMethod);
    }

    [Fact]
    public void AlAnsari_rejected_nonce_is_explained()
    {
        var ex = Assert.Throws<FormatException>(() => AlAnsariParser.Parse("-1", Fixture.KwdBdt, Fixture.FetchedAt));
        Assert.Contains("nonce", ex.Message);
    }

    [Fact]
    public void Aman_reads_the_transfer_rate_and_kuwait_time()
    {
        var rate = AmanParser.Parse(Fixture.Read("aman-rate.json"), Fixture.KwdBdt, Fixture.FetchedAt);

        Assert.Equal(385.3817m, rate.Rate);
        Assert.Equal(new DateTime(2026, 10, 17, 11, 54, 17, 824, DateTimeKind.Utc).AddTicks(5000), rate.PublishedAt);
    }

    // ---------------------------------------------------------------- Shared rules

    [Fact]
    public void Zero_rate_is_rejected()
    {
        Assert.Throws<FormatException>(() =>
            PostedRates.Create("Test", Fixture.KwdBdt, 0m, Labels.BankDeposit, Fixture.FetchedAt));
    }

    [Fact]
    public void Failed_exchange_house_carries_its_last_rate_forward_as_stale()
    {
        var previous = SnapshotBuilder.Build(Fixture.OmrBdt, [], null, null, Fixture.FetchedAt,
            [PostedRateResult.Ok("Unimoni", [PostedRates.Create("Unimoni", Fixture.OmrBdt, 319.9995m, Labels.NotSpecified, Fixture.FetchedAt)])]);
        var later = Fixture.FetchedAt.AddDays(1);

        var snapshot = SnapshotBuilder.Build(Fixture.OmrBdt, [], null, previous, later,
        [
            PostedRateResult.Failed("Unimoni", "HttpRequestException: 503"),
            PostedRateResult.Ok("PKExchange", [PostedRates.Create("PKExchange", Fixture.OmrBdt, 320m, Labels.BankDeposit, later)]),
        ]);

        Assert.Equal(["PKExchange", "Unimoni"], snapshot.PostedRates.Select(r => r.Provider)); // best rate first
        var carried = Assert.Single(snapshot.PostedRates, r => r.Provider == "Unimoni");
        Assert.True(carried.Stale);
        Assert.Equal(Fixture.FetchedAt, carried.FetchedAt);
        var status = Assert.Single(snapshot.PostedRateProviders, s => s.Name == "Unimoni");
        Assert.Equal(ProviderStatus.Stale, status.Status);
        Assert.Contains("last good rate", status.Message);
        Assert.Empty(snapshot.Providers);
    }

    private sealed class ScopedFake(string name, IReadOnlySet<string> countries) : IRateProvider, ISendCountryScoped
    {
        public string Name => name;

        public IReadOnlySet<string> SendCountries => countries;

        public Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Quote>>(sendAmounts
                .Select(a => new Quote(name, c.Id, c.SendCurrency, a, 400m, 1m, (a - 1m) * 400m, "Bank deposit", "Within 1 day",
                    DateTime.UtcNow, false))
                .ToList());
    }

    private sealed class PostedFake(string name, IReadOnlySet<string> countries) : IPostedRateProvider
    {
        public string Name => name;

        public IReadOnlySet<string> SendCountries => countries;

        public Task<IReadOnlyList<PostedRate>> GetRatesAsync(Corridor c, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PostedRate>>([PostedRates.Create(name, c, 398m, Labels.BankDeposit, DateTime.UtcNow)]);
    }

    [Fact]
    public async Task Country_scoped_providers_are_skipped_elsewhere_without_a_status()
    {
        var options = new EngineOptions
        {
            SendAmounts = [100],
            Corridors =
            [
                new CorridorOptions { Id = "gbp-bdt", SendCurrency = "GBP", ReceiveCurrency = "BDT", SendCountry = "GB", ReceiveCountry = "BD" },
                new CorridorOptions { Id = "kwd-bdt", SendCurrency = "KWD", ReceiveCurrency = "BDT", SendCountry = "KW", ReceiveCountry = "BD" },
            ],
        };
        var kuwait = new HashSet<string> { "KW" };
        var runner = new RateRunner([new ScopedFake("WesternUnion", kuwait)], [new PostedFake("Aman", kuwait)],
            new JsonDataStore(_root), Microsoft.Extensions.Options.Options.Create(options), TimeProvider.System,
            NullLogger<RateRunner>.Instance);

        var snapshots = await runner.RunAsync(CancellationToken.None);

        var uk = snapshots.Single(s => s.Corridor == "gbp-bdt");
        Assert.Empty(uk.Providers);
        Assert.Empty(uk.PostedRateProviders);
        Assert.Empty(uk.PostedRates);

        var kw = snapshots.Single(s => s.Corridor == "kwd-bdt");
        Assert.Equal(ProviderStatus.Ok, Assert.Single(kw.Providers).Status);
        Assert.Equal(ProviderStatus.Ok, Assert.Single(kw.PostedRateProviders).Status);
        Assert.Equal(398m, Assert.Single(kw.PostedRates).Rate);
    }

    [Fact]
    public void Snapshot_written_before_posted_rates_existed_reads_as_empty_lists()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "gbp-bdt.json"), """
            {"corridor":"gbp-bdt","sendCurrency":"GBP","receiveCurrency":"BDT","generatedAt":"2026-10-17T03:00:00Z",
             "midMarket":null,"providers":[],"quotes":[]}
            """);

        var snapshot = new JsonDataStore(_root).ReadLatest("gbp-bdt");

        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.PostedRateProviders);
        Assert.Empty(snapshot.PostedRates);
    }

    // ---------------------------------------------------------------- Review follow-ups

    [Fact]
    public void PKExchange_accepts_single_digit_dates_and_rejects_a_missing_one()
    {
        var html = Fixture.Read("pkexchange-rates.html");

        var rate = PKExchangeParser.Parse(html.Replace("17/10/2026", "7/10/2026"), Fixture.OmrBdt, "BANGLADESH",
            new DateTime(2026, 10, 7, 8, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 10, 6, 20, 0, 0, DateTimeKind.Utc), rate.PublishedAt);

        var ex = Assert.Throws<FormatException>(() =>
            PKExchangeParser.Parse(html.Replace("17/10/2026", "Saturday"), Fixture.OmrBdt, "BANGLADESH", Fixture.FetchedAt));
        Assert.Contains("no readable date", ex.Message);
    }

    [Fact]
    public void Aman_honours_an_explicit_offset_and_rejects_a_missing_date()
    {
        var json = Fixture.Read("aman-rate.json");

        var withOffset = AmanParser.Parse(json.Replace("2026-10-17T14:54:17.8245", "2026-10-17T14:54:17+03:00"), Fixture.KwdBdt,
            Fixture.FetchedAt);
        Assert.Equal(new DateTime(2026, 10, 17, 11, 54, 17, DateTimeKind.Utc), withOffset.PublishedAt);

        Assert.Throws<FormatException>(() =>
            AmanParser.Parse(json.Replace("\"RateDate\":\"2026-10-17T14:54:17.8245\"", "\"RateDate\":null"), Fixture.KwdBdt,
                Fixture.FetchedAt));
    }

    [Fact]
    public void Joyalukkas_rejects_an_answer_in_another_currency()
    {
        // The Kuwait (region 1) answer is quoted in KWD; read as Oman it must not become an OMR rate.
        var ex = Assert.Throws<FormatException>(() =>
            JoyalukkasParser.Parse(Fixture.Read("joyalukkas-region1-bdt.json"), Fixture.OmrBdt, Fixture.FetchedAt));
        Assert.Contains("KWD", ex.Message);
    }

    [Fact]
    public void AlAnsari_error_page_is_cut_short()
    {
        var page = "<!DOCTYPE html>\n<html><head><title>Service unavailable</title></head><body>" + new string('x', 5000) + "</body></html>";

        var ex = Assert.Throws<FormatException>(() => AlAnsariParser.Parse(page, Fixture.KwdBdt, Fixture.FetchedAt));
        Assert.True(ex.Message.Length < 160, ex.Message);
        Assert.DoesNotContain("\n", ex.Message);
    }

    [Fact]
    public async Task AlAnsari_http_error_is_reported_as_such()
    {
        var handler = new FakeHandler(request => request.Method == HttpMethod.Get
            ? new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(Fixture.Read("alansari-home-excerpt.html")) }
            : new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable) { Content = new StringContent("<html>busy</html>") });

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            new AlAnsariProvider(new HttpClient(handler), TimeProvider.System).GetRatesAsync(Fixture.KwdBdt, CancellationToken.None));
        Assert.Contains("503", ex.Message);
        Assert.Contains("security=76a2864716", handler.Bodies[1]);
    }

    [Fact]
    public void Posted_rates_far_from_the_benchmark_are_dropped()
    {
        var mid = new MidMarketInfo(397.138m, 123.45m, "2026-10-17", "Bangladesh Bank", 0.3108m, "2026-10-17", "currency-api",
            Fixture.FetchedAt, Stale: false);
        var plausible = PostedRates.Create("Joyalukkas", Fixture.KwdBdt, 400.8765m, Labels.BankDeposit, Fixture.FetchedAt);
        var aedRate = PostedRates.Create("Joyalukkas", Fixture.KwdBdt, 33.40m, Labels.BankDeposit, Fixture.FetchedAt);

        var snapshot = SnapshotBuilder.Build(Fixture.KwdBdt, [], mid, null, Fixture.FetchedAt,
        [
            PostedRateResult.Ok("Joyalukkas", [aedRate]),
            PostedRateResult.Ok("Ooredoo", [plausible with { Provider = "Ooredoo" }, aedRate with { Provider = "Ooredoo" }]),
        ]);

        var joyalukkas = Assert.Single(snapshot.PostedRateProviders, s => s.Name == "Joyalukkas");
        Assert.Equal(ProviderStatus.Failed, joyalukkas.Status);
        Assert.Contains("Implausible", joyalukkas.Message);
        var kept = Assert.Single(snapshot.PostedRates);
        Assert.Equal(400.8765m, kept.Rate);
        Assert.Contains("Dropped 1", Assert.Single(snapshot.PostedRateProviders, s => s.Name == "Ooredoo").Message);
    }
}
