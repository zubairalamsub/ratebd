using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

public class WiseParserTests
{
    [Theory]
    [InlineData("wise-gbp-bdt-1000.json", 1000, 8.86, 162463.70)]
    [InlineData("wise-gbp-bdt-500.json", 500, 5.28, 81092.52)]
    public void Parses_bank_transfer_option(string fixture, decimal amount, decimal fee, decimal receive)
    {
        var quote = WiseParser.Parse(Fixture.Read(fixture), Fixture.GbpBdt, amount, Fixture.FetchedAt);

        Assert.Equal("Wise", quote.Provider);
        Assert.Equal("gbp-bdt", quote.Corridor);
        Assert.Equal(amount, quote.SendAmount);
        Assert.Equal(163.916m, quote.Rate);
        Assert.Equal(fee, quote.Fee);
        Assert.Equal(receive, quote.ReceiveAmount);
        Assert.Equal("Bank deposit", quote.DeliveryMethod);
        Assert.Equal("Within 2 days", quote.Speed); // created 07:51 UTC, delivery estimated next day 12:00 UTC
        Assert.False(quote.Stale);
    }

    [Fact]
    public void Aed_with_only_disabled_options_is_not_supported()
    {
        var ex = Assert.Throws<ProviderNotSupportedException>(() =>
            WiseParser.Parse(Fixture.Read("wise-aed-bdt-1000.json"), Fixture.AedBdt, 1000, Fixture.FetchedAt));

        Assert.Contains("error.payInmethod.disabled", ex.Message);
    }

    [Fact]
    public void Malformed_response_throws_format_exception()
    {
        Assert.Throws<FormatException>(() =>
            WiseParser.Parse("""{"unexpected":true}""", Fixture.GbpBdt, 1000, Fixture.FetchedAt));
    }
}
