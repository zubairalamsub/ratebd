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

    [Theory]
    [InlineData("habibqatar", 33.90, "Not specified")]
    [InlineData("almirqab", 33.88, "Bank deposit")]
    public void Qatar_rate_boards(string source, decimal rate, string delivery)
    {
        var posted = source switch
        {
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

    // ---------------------------------------------------------------- Kuwait

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
    public void Posted_rates_far_from_the_benchmark_are_dropped()
    {
        var mid = new MidMarketInfo(397.138m, 123.45m, "2026-10-17", "Bangladesh Bank", 0.3108m, "2026-10-17", "currency-api",
            Fixture.FetchedAt, Stale: false);
        var plausible = PostedRates.Create("Aman", Fixture.KwdBdt, 400.8765m, Labels.BankDeposit, Fixture.FetchedAt);
        var aedRate = PostedRates.Create("Aman", Fixture.KwdBdt, 33.40m, Labels.BankDeposit, Fixture.FetchedAt);

        var snapshot = SnapshotBuilder.Build(Fixture.KwdBdt, [], mid, null, Fixture.FetchedAt,
        [
            PostedRateResult.Ok("Aman", [aedRate]),
            PostedRateResult.Ok("Unimoni", [plausible with { Provider = "Unimoni" }, aedRate with { Provider = "Unimoni" }]),
        ]);

        var aman = Assert.Single(snapshot.PostedRateProviders, s => s.Name == "Aman");
        Assert.Equal(ProviderStatus.Failed, aman.Status);
        Assert.Contains("Implausible", aman.Message);
        var kept = Assert.Single(snapshot.PostedRates);
        Assert.Equal(400.8765m, kept.Rate);
        Assert.Contains("Dropped 1", Assert.Single(snapshot.PostedRateProviders, s => s.Name == "Unimoni").Message);
    }
}
