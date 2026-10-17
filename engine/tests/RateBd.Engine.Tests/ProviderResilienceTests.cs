using Microsoft.Extensions.Logging.Abstractions;
using Polly.Timeout;
using RateBd.Engine.Options;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

/// <summary>
/// The resilience handler reports timeouts as Polly's TimeoutRejectedException; these paths must degrade gracefully.
/// </summary>
public class ProviderResilienceTests
{
    [Fact]
    public async Task MidMarket_falls_back_to_second_mirror_when_first_times_out()
    {
        var handler = new FakeHandler(request => request.RequestUri!.Host == "mirror-one.example"
            ? throw new TimeoutRejectedException("timed out")
            : FakeHandler.Json(Fixture.Read("currency-api-usd.json")));
        var options = Microsoft.Extensions.Options.Options.Create(new MidMarketOptions
        {
            BangladeshBankHtmlPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "bangladesh-bank-exchangerate.html"),
            CrossRateUrls = ["https://mirror-one.example/usd.json", "https://mirror-two.example/usd.json"],
        });
        var provider = new MidMarketProvider(new HttpClient(handler), TimeProvider.System, options,
            NullLogger<MidMarketProvider>.Instance);

        var quotes = await provider.GetQuotesAsync(Fixture.GbpBdt, [1000m], CancellationToken.None);

        Assert.Single(quotes);
        Assert.Equal(["mirror-one.example", "mirror-two.example"], handler.Requests.Select(r => r.RequestUri!.Host));
        Assert.NotNull(provider.GetBenchmark("gbp-bdt"));
    }

    [Fact]
    public void Default_data_root_accepts_git_file_of_a_linked_worktree()
    {
        var root = Path.Combine(Path.GetTempPath(), "ratebd-tests", Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "engine", "src");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(root, ".git"), "gitdir: /elsewhere/.git/worktrees/x\n");
        try
        {
            Assert.Equal(Path.Combine(root, "data"), Storage.JsonDataStore.LocateDefaultRoot(nested));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
