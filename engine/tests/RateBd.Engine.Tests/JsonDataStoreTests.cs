using RateBd.Engine.Models;
using RateBd.Engine.Storage;

namespace RateBd.Engine.Tests;

public sealed class JsonDataStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ratebd-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Write_creates_latest_and_dated_history_file_that_round_trip()
    {
        var store = new JsonDataStore(_root);
        var generatedAt = new DateTime(2026, 10, 17, 3, 0, 5, DateTimeKind.Utc);
        var snapshot = new CorridorSnapshot("gbp-bdt", "GBP", "BDT", generatedAt, null,
            [new ProviderStatus("Wise", ProviderStatus.Ok, null)],
            [Fixture.Quote("Wise", 1000, 162463.70m)]);

        store.Write(snapshot);

        Assert.True(File.Exists(Path.Combine(_root, "gbp-bdt.json")));
        Assert.True(File.Exists(Path.Combine(_root, "history", "gbp-bdt", "2026-10-17.json")));

        var read = store.ReadLatest("gbp-bdt");
        Assert.NotNull(read);
        Assert.Equal(generatedAt, read.GeneratedAt);
        Assert.Equal(DateTimeKind.Utc, read.GeneratedAt.Kind);
        var quote = Assert.Single(read.Quotes);
        Assert.Equal(162463.70m, quote.ReceiveAmount);
        Assert.Equal(Fixture.FetchedAt, quote.FetchedAt);
    }

    [Fact]
    public void Json_uses_camel_case_property_names()
    {
        var store = new JsonDataStore(_root);
        store.Write(new CorridorSnapshot("aed-bdt", "AED", "BDT", Fixture.FetchedAt, null, [], []));

        var json = File.ReadAllText(store.LatestPath("aed-bdt"));
        Assert.Contains("\"generatedAt\"", json);
        Assert.Contains("\"sendCurrency\": \"AED\"", json);
    }

    [Fact]
    public void ReadLatest_returns_null_when_nothing_written_yet()
    {
        Assert.Null(new JsonDataStore(_root).ReadLatest("gbp-bdt"));
    }
}
