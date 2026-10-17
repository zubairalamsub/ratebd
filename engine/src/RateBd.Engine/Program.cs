using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateBd.Engine;
using RateBd.Engine.Options;
using RateBd.Engine.Providers;
using RateBd.Engine.Storage;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    // appsettings.json ships next to the binary; the working directory is wherever CI runs it from.
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Configuration.AddEnvironmentVariables(prefix: "RATEBD_");

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.UseUtcTimestamp = true;
    o.TimestampFormat = "HH:mm:ss ";
});

builder.Services.Configure<EngineOptions>(builder.Configuration.GetSection(EngineOptions.Section));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp =>
{
    var configured = sp.GetRequiredService<IOptions<EngineOptions>>().Value.DataDirectory;
    return new JsonDataStore(string.IsNullOrWhiteSpace(configured)
        ? JsonDataStore.LocateDefaultRoot(Directory.GetCurrentDirectory())
        : configured);
});
builder.Services.AddSingleton<RateRunner>();
builder.Services.AddRateProviders(builder.Configuration);

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var snapshots = await host.Services.GetRequiredService<RateRunner>().RunAsync(cts.Token);

if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } summaryPath)
{
    await File.AppendAllTextAsync(summaryPath, RateRunner.Summarise(snapshots), cts.Token);
}

var fresh = snapshots.Sum(s => s.Providers.Concat(s.PostedRateProviders)
    .Count(p => p.Status == "ok" && p.Name != "MidMarket"));
if (fresh == 0 && host.Services.GetRequiredService<IOptions<EngineOptions>>().Value.FailWhenNothingFetched)
{
    logger.LogCritical("No provider returned a fresh quote on any corridor; data was written with stale values only");
    return 2;
}
return 0;
