using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

public class MidMarketTests
{
    [Fact]
    public void Bangladesh_bank_page_yields_usd_war_and_effective_date()
    {
        var rate = BangladeshBankParser.Parse(Fixture.Read("bangladesh-bank-exchangerate.html"));

        Assert.Equal(123.4500m, rate.UsdBdt);
        Assert.Equal("2026-10-17", rate.EffectiveDate);
    }

    [Fact]
    public void Bot_challenge_page_is_reported_clearly()
    {
        var ex = Assert.Throws<FormatException>(() =>
            BangladeshBankParser.Parse(Fixture.Read("bangladesh-bank-challenge.html")));

        Assert.Contains("bot challenge", ex.Message);
    }

    [Fact]
    public void Currency_api_feed_gives_units_per_usd()
    {
        var cross = CurrencyApiParser.Parse(Fixture.Read("currency-api-usd.json"));

        Assert.NotNull(cross.Date);
        Assert.InRange(cross.For("GBP"), 0.5m, 1m);
        Assert.Equal(3.6725m, cross.For("AED"));
        Assert.Throws<FormatException>(() => cross.For("XYZ"));
    }

    [Fact]
    public void Mid_rate_is_usd_bdt_divided_by_send_units_per_usd()
    {
        var bb = new BangladeshBankRate(123.45m, "2026-10-17");
        var cross = new UsdCrossRates("2026-10-16", new Dictionary<string, decimal> { ["gbp"] = 0.75m, ["aed"] = 3.6725m });

        var gbp = MidMarketProvider.Calculate(Fixture.GbpBdt, bb, cross, Fixture.FetchedAt);
        var aed = MidMarketProvider.Calculate(Fixture.AedBdt, bb, cross, Fixture.FetchedAt);

        Assert.Equal(164.6000m, gbp.Rate);
        Assert.Equal(33.6147m, aed.Rate); // 123.45 / 3.6725 = 33.61470...
        Assert.Equal(123.45m, gbp.UsdBdt);
        Assert.Equal("2026-10-17", gbp.UsdBdtDate);
        Assert.Equal(0.75m, gbp.SendPerUsd);
        Assert.False(gbp.Stale);
    }

    [Theory]
    [InlineData("usd", 1)]
    [InlineData("eur", 0)]
    [InlineData("myr", 0)]
    [InlineData("sgd", 0)]
    public void MidMarket_crosses_every_new_currency(string from, int isUsd)
    {
        var corridor = from switch { "usd" => Fixture.UsdBdt, "eur" => Fixture.EurBdt, "myr" => Fixture.MyrBdt, _ => Fixture.SgdBdt };
        var cross = CurrencyApiParser.Parse(Fixture.Read("currency-api-usd.json"));
        var bb = new BangladeshBankRate(123.45m, "2026-10-17");

        var info = MidMarketProvider.Calculate(corridor, bb, cross, Fixture.FetchedAt);

        Assert.Equal(isUsd == 1 ? 1m : cross.For(corridor.SendCurrency), info.SendPerUsd);
        Assert.Equal(Math.Round(123.45m / info.SendPerUsd, 4, MidpointRounding.AwayFromZero), info.Rate);
    }
}
