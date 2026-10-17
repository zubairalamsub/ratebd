using RateBd.Engine.Models;
using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

public class TaptapSendParserTests
{
    private static readonly decimal[] Amounts = [500, 1000];

    [Fact]
    public void Gbp_to_bdt_uses_corridor_rate_for_each_amount()
    {
        var quotes = TaptapSendParser.Parse(Fixture.Read("taptapsend-fxrates.json"), Fixture.GbpBdt, Amounts, Fixture.FetchedAt);

        Assert.Equal(2, quotes.Count);
        Assert.All(quotes, q =>
        {
            Assert.Equal("TaptapSend", q.Provider);
            Assert.Equal(163.00m, q.Rate);
            Assert.Equal(0m, q.Fee);
            Assert.Equal(q.SendAmount * 163.00m, q.ReceiveAmount);
            Assert.Equal("Not specified", q.DeliveryMethod);
            Assert.Equal("Not specified", q.Speed);
        });
    }

    [Fact]
    public void Aed_to_bdt_is_supported()
    {
        var quotes = TaptapSendParser.Parse(Fixture.Read("taptapsend-fxrates.json"), Fixture.AedBdt, Amounts, Fixture.FetchedAt);

        Assert.Equal(33.65m, quotes[0].Rate);
        Assert.Equal(16825.00m, quotes[0].ReceiveAmount);
    }

    [Fact]
    public void Tiered_fee_schedule_picks_highest_matching_tier()
    {
        // GB→PK in the same captured response: 0.99 below £125, free from £125.
        var gbpPkr = new Corridor("gbp-pkr", "GBP", "PKR", "GB", "PK");
        var quotes = TaptapSendParser.Parse(Fixture.Read("taptapsend-fxrates.json"), gbpPkr, [100m, 500m], Fixture.FetchedAt);

        Assert.Equal(0.99m, quotes[0].Fee);
        Assert.Equal(0m, quotes[1].Fee);
    }

    [Fact]
    public void Standard_fee_schedule_uses_flat_fee()
    {
        var gbpArs = new Corridor("gbp-ars", "GBP", "ARS", "GB", "AR");
        var quote = TaptapSendParser.Parse(Fixture.Read("taptapsend-fxrates.json"), gbpArs, [500m], Fixture.FetchedAt)[0];

        Assert.Equal(1.99m, quote.Fee);
        Assert.Equal(Math.Round((500m - 1.99m) * quote.Rate, 2), quote.ReceiveAmount);
    }

    [Fact]
    public void Unknown_corridor_is_not_supported()
    {
        var gbpXyz = new Corridor("gbp-xyz", "GBP", "XYZ", "GB", "XY");

        Assert.Throws<ProviderNotSupportedException>(() =>
            TaptapSendParser.Parse(Fixture.Read("taptapsend-fxrates.json"), gbpXyz, Amounts, Fixture.FetchedAt));
    }
}
