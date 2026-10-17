using Microsoft.Extensions.Logging.Abstractions;
using RateBd.Engine.Models;
using RateBd.Engine.Options;
using RateBd.Engine.Providers;
using RateBd.Engine.Storage;

namespace RateBd.Engine.Tests;

public sealed class RateRunnerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ratebd-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class FakeProvider(string name, Func<Corridor, decimal[], IReadOnlyList<Quote>> fetch) : IRateProvider
    {
        public string Name => name;

        public Task<IReadOnlyList<Quote>> GetQuotesAsync(Corridor c, decimal[] sendAmounts, CancellationToken ct) =>
            Task.FromResult(fetch(c, sendAmounts));
    }

    private RateRunner Runner(params IRateProvider[] providers)
    {
        var options = new EngineOptions
        {
            SendAmounts = [500, 1000],
            Corridors =
            [
                new CorridorOptions { Id = "gbp-bdt", SendCurrency = "GBP", ReceiveCurrency = "BDT", SendCountry = "GB", ReceiveCountry = "BD" },
                new CorridorOptions { Id = "aed-bdt", SendCurrency = "AED", ReceiveCurrency = "BDT", SendCountry = "AE", ReceiveCountry = "BD" },
            ],
        };
        return new RateRunner(providers, new JsonDataStore(_root), Microsoft.Extensions.Options.Options.Create(options),
            TimeProvider.System, NullLogger<RateRunner>.Instance);
    }

    private static IReadOnlyList<Quote> QuotesFor(string provider, Corridor c, decimal[] amounts) =>
        amounts.Select(a => new Quote(provider, c.Id, c.SendCurrency, a, 2m, 0m, a * 2m, "Bank deposit",
            "Within 1 day", DateTime.UtcNow, false)).ToList();

    [Fact]
    public async Task A_throwing_provider_does_not_stop_the_run()
    {
        var runner = Runner(
            new FakeProvider("Broken", (_, _) => throw new HttpRequestException("403 Forbidden")),
            new FakeProvider("Good", (c, a) => QuotesFor("Good", c, a)));

        var snapshots = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(2, snapshots.Count);
        foreach (var snapshot in snapshots)
        {
            Assert.Equal(2, snapshot.Quotes.Count(q => q.Provider == "Good"));
            var broken = Assert.Single(snapshot.Providers, p => p.Name == "Broken");
            Assert.Equal(ProviderStatus.Failed, broken.Status);
            Assert.Contains("403", broken.Message);
        }
        Assert.True(File.Exists(Path.Combine(_root, "gbp-bdt.json")));
        Assert.True(File.Exists(Path.Combine(_root, "aed-bdt.json")));
    }

    [Fact]
    public async Task Unsupported_corridor_is_recorded_as_not_supported()
    {
        var runner = Runner(new FakeProvider("GbOnly", (c, a) => c.SendCountry == "GB"
            ? QuotesFor("GbOnly", c, a)
            : throw new ProviderNotSupportedException("no UAE product")));

        var snapshots = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(ProviderStatus.Ok, snapshots[0].Providers[0].Status);
        Assert.Equal(ProviderStatus.NotSupported, snapshots[1].Providers[0].Status);
        Assert.Equal("no UAE product", snapshots[1].Providers[0].Message);
    }

    [Fact]
    public async Task Second_run_with_failure_carries_forward_first_run_quotes()
    {
        await Runner(new FakeProvider("Flaky", (c, a) => QuotesFor("Flaky", c, a))).RunAsync(CancellationToken.None);
        var second = await Runner(new FakeProvider("Flaky", (_, _) => throw new TimeoutException("slow")))
            .RunAsync(CancellationToken.None);

        Assert.All(second, s =>
        {
            Assert.Equal(ProviderStatus.Stale, s.Providers[0].Status);
            Assert.Equal(2, s.Quotes.Count);
            Assert.All(s.Quotes, q => Assert.True(q.Stale));
        });
    }

    [Fact]
    public async Task Empty_result_counts_as_failure()
    {
        var snapshots = await Runner(new FakeProvider("Empty", (_, _) => [])).RunAsync(CancellationToken.None);

        Assert.All(snapshots, s => Assert.Equal(ProviderStatus.Failed, s.Providers[0].Status));
    }
}
