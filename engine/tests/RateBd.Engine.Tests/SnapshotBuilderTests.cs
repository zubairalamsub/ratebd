using RateBd.Engine.Models;

namespace RateBd.Engine.Tests;

public class SnapshotBuilderTests
{
    private static readonly DateTime Now = new(2026, 10, 18, 3, 0, 0, DateTimeKind.Utc);

    private static CorridorSnapshot Previous() => new(
        "gbp-bdt", "GBP", "BDT", Fixture.FetchedAt,
        new MidMarketInfo(163.9m, 123.45m, "2026-10-17", "Bangladesh Bank", 0.7531m, "2026-10-16", "currency-api",
            Fixture.FetchedAt, Stale: false),
        [new ProviderStatus("Wise", ProviderStatus.Ok, null), new ProviderStatus("Remitly", ProviderStatus.Ok, null)],
        [Fixture.Quote("Wise", 1000, 162463.70m), Fixture.Quote("Remitly", 1000, 163228.24m)]);

    [Fact]
    public void Failed_provider_carries_forward_last_good_quote_as_stale()
    {
        var fresh = Fixture.Quote("Wise", 1000, 162500m, Now);
        var snapshot = SnapshotBuilder.Build(Fixture.GbpBdt,
            [ProviderResult.Ok("Wise", [fresh]), ProviderResult.Failed("Remitly", "HttpRequestException: 429")],
            benchmark: null, Previous(), Now);

        var remitly = Assert.Single(snapshot.Quotes, q => q.Provider == "Remitly");
        Assert.True(remitly.Stale);
        Assert.Equal(Fixture.FetchedAt, remitly.FetchedAt);
        Assert.Equal(163228.24m, remitly.ReceiveAmount);

        var status = Assert.Single(snapshot.Providers, p => p.Name == "Remitly");
        Assert.Equal(ProviderStatus.Stale, status.Status);
        Assert.Contains("429", status.Message);

        Assert.False(Assert.Single(snapshot.Quotes, q => q.Provider == "Wise").Stale);
    }

    [Fact]
    public void Failed_provider_without_history_is_marked_failed()
    {
        var snapshot = SnapshotBuilder.Build(Fixture.GbpBdt,
            [ProviderResult.Failed("Xe", "boom")], benchmark: null, previous: null, Now);

        Assert.Empty(snapshot.Quotes);
        Assert.Equal(ProviderStatus.Failed, Assert.Single(snapshot.Providers).Status);
    }

    [Fact]
    public void Not_supported_provider_is_not_carried_forward()
    {
        var snapshot = SnapshotBuilder.Build(Fixture.GbpBdt,
            [ProviderResult.NotSupported("Remitly", "no source")], benchmark: null, Previous(), Now);

        Assert.DoesNotContain(snapshot.Quotes, q => q.Provider == "Remitly");
        Assert.Equal(ProviderStatus.NotSupported, Assert.Single(snapshot.Providers).Status);
    }

    [Fact]
    public void Missing_benchmark_falls_back_to_previous_marked_stale()
    {
        var snapshot = SnapshotBuilder.Build(Fixture.GbpBdt, [], benchmark: null, Previous(), Now);

        Assert.NotNull(snapshot.MidMarket);
        Assert.True(snapshot.MidMarket.Stale);
        Assert.Equal(163.9m, snapshot.MidMarket.Rate);
    }

    [Fact]
    public void Quotes_are_sorted_by_amount_then_receive_amount_descending()
    {
        var snapshot = SnapshotBuilder.Build(Fixture.GbpBdt,
        [
            ProviderResult.Ok("A", [Fixture.Quote("A", 1000, 100m), Fixture.Quote("A", 500, 50m)]),
            ProviderResult.Ok("B", [Fixture.Quote("B", 1000, 200m), Fixture.Quote("B", 500, 40m)]),
        ], benchmark: null, previous: null, Now);

        Assert.Equal(["A-500", "B-500", "B-1000", "A-1000"],
            snapshot.Quotes.Select(q => $"{q.Provider}-{q.SendAmount}"));
    }
}
