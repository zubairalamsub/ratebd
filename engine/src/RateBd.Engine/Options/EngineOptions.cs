using RateBd.Engine.Models;

namespace RateBd.Engine.Options;

public sealed class EngineOptions
{
    public const string Section = "Engine";

    /// <summary>Where /data lives. Relative paths resolve against the current directory;
    /// empty means "the data folder at the root of the git checkout containing the current directory".</summary>
    public string? DataDirectory { get; set; }

    /// <summary>Default amounts for corridors that do not set their own.</summary>
    public decimal[] SendAmounts { get; set; } = [];

    public List<CorridorOptions> Corridors { get; set; } = [];

    /// <summary>Upper bound for one provider on one corridor, including all retries.</summary>
    public int ProviderTimeoutSeconds { get; set; } = 90;

    /// <summary>Exit with a non-zero code when no provider returned a fresh quote on any corridor.</summary>
    public bool FailWhenNothingFetched { get; set; } = true;

    /// <summary>When set, only these providers run (e.g. <c>["MidMarket"]</c> for an offline smoke test).</summary>
    public string[] OnlyProviders { get; set; } = [];
}

public sealed class CorridorOptions
{
    public string Id { get; set; } = "";
    public string SendCurrency { get; set; } = "";
    public string ReceiveCurrency { get; set; } = "";
    public string SendCountry { get; set; } = "";
    public string ReceiveCountry { get; set; } = "";

    /// <summary>Amounts quoted on this corridor; empty means <see cref="EngineOptions.SendAmounts"/>.</summary>
    public decimal[] SendAmounts { get; set; } = [];

    public Corridor ToCorridor() => new(Id, SendCurrency, ReceiveCurrency, SendCountry, ReceiveCountry);
}

/// <summary>Per-provider switches, bound from <c>Providers:{Name}</c>.</summary>
public sealed class ProviderOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Optional API key. No v1 provider needs one; a provider that does must skip gracefully when it is empty.</summary>
    public string? ApiKey { get; set; }
}

public sealed class MidMarketOptions
{
    public const string Section = "Providers:MidMarket";

    /// <summary>HTML saved by tools/bb-snapshot. When empty the provider requests the page directly,
    /// which normally only returns Bangladesh Bank's JavaScript bot challenge.</summary>
    public string? BangladeshBankHtmlPath { get; set; }

    /// <summary>fawazahmed0/currency-api mirrors (USD base), tried in order.</summary>
    public string[] CrossRateUrls { get; set; } = [];
}
