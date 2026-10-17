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
    public async Task Remitly_continues_without_device_id_when_page_times_out_and_does_not_refetch_it()
    {
        var handler = new FakeHandler(request => request.RequestUri!.Host == "www.remitly.com"
            ? throw new TimeoutRejectedException("timed out")
            : FakeHandler.Json(Fixture.Read("remitly-gbp-bdt-500.json")));
        var provider = new RemitlyProvider(new HttpClient(handler), TimeProvider.System, NullLogger<RemitlyProvider>.Instance);

        var first = await provider.GetQuotesAsync(Fixture.GbpBdt, [500m], CancellationToken.None);
        var second = await provider.GetQuotesAsync(Fixture.GbpBdt, [500m], CancellationToken.None);

        Assert.Equal(163.39m, first[0].Rate);
        Assert.Single(second);
        Assert.Equal(1, handler.Requests.Count(r => r.RequestUri!.Host == "www.remitly.com"));
        var estimate = handler.Requests.First(r => r.RequestUri!.Host == "api.remitly.com");
        Assert.False(estimate.Headers.Contains("Remitly-DeviceEnvironmentID"));
        Assert.Equal("app_id_core", estimate.Headers.GetValues("remittance-distro-id").Single());
    }

    [Fact]
    public async Task Remitly_sends_device_id_read_from_the_page()
    {
        var handler = new FakeHandler(request => request.RequestUri!.Host == "www.remitly.com"
            ? FakeHandler.Json(Fixture.Read("remitly-converter-page-excerpt.html"))
            : FakeHandler.Json(Fixture.Read("remitly-gbp-bdt-500.json")));
        var provider = new RemitlyProvider(new HttpClient(handler), TimeProvider.System, NullLogger<RemitlyProvider>.Instance);

        await provider.GetQuotesAsync(Fixture.GbpBdt, [500m], CancellationToken.None);

        Assert.Equal("https://www.remitly.com/gb/en/currency-converter/gbp-to-bdt-rate", handler.Requests[0].RequestUri!.ToString());
        var estimate = handler.Requests[1];
        Assert.Equal("REDACTEDDEVICEENVIRONMENTIDFORTESTS0123456789",
            estimate.Headers.GetValues("Remitly-DeviceEnvironmentID").Single());
        Assert.Contains("customer_segment=STANDARD", estimate.RequestUri!.Query);
        Assert.Contains("conduit=GBR%3AGBP-BGD%3ABDT", estimate.RequestUri!.Query);
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
