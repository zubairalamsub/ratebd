using RateBd.Engine.Parsing;

namespace RateBd.Engine.Tests;

public class XeParserTests
{
    [Theory]
    [InlineData("xe-gbp-bdt-1000.json", 1000, 0.00, 163926.20)]
    [InlineData("xe-gbp-bdt-500.json", 500, 3.00, 81471.32)] // (500 - 3) * 163.9262
    public void Picks_default_enabled_option(string fixture, decimal amount, decimal fee, decimal receive)
    {
        var quote = XeParser.Parse(Fixture.Read(fixture), Fixture.GbpBdt, amount, Fixture.FetchedAt);

        Assert.Equal("Xe", quote.Provider);
        Assert.Equal(163.9262m, quote.Rate);
        Assert.Equal(fee, quote.Fee);
        Assert.Equal(receive, quote.ReceiveAmount);
        Assert.Equal("Bank deposit", quote.DeliveryMethod);
        Assert.Equal("Typically within 48 hours", quote.Speed);
    }

    [Fact]
    public void No_enabled_option_throws()
    {
        const string json = """{"quote":{"individualQuotes":[{"isEnabled":false,"rate":1}],"errorMessages":[]}}""";

        Assert.Throws<FormatException>(() => XeParser.Parse(json, Fixture.GbpBdt, 500, Fixture.FetchedAt));
    }
}
