using RateBd.Engine.Parsing;

namespace RateBd.Engine.Tests;

public class WorldRemitParserTests
{
    private static readonly WorldRemitPayOutMethod Bkash = new("MOB", "4004", "bKash Mobile Money", "Same day");

    [Fact]
    public void Payout_methods_skip_airtime_and_clean_speed()
    {
        var methods = WorldRemitParser.ParsePayOutMethods(Fixture.Read("worldremit-payoutmethods-gb-bd.json"));

        Assert.Equal(["MOB", "PTV"], methods.Select(m => m.Code));
        Assert.Equal(Bkash, methods[0]);
        Assert.Equal("Within 1 hour", methods[1].Speed);
        Assert.Equal("Debit Card Deposit", methods[1].CorrespondentName);
    }

    [Fact]
    public void Uae_has_no_payout_methods()
    {
        Assert.Empty(WorldRemitParser.ParsePayOutMethods(Fixture.Read("worldremit-payoutmethods-ae-bd.json")));
    }

    [Theory]
    [InlineData("worldremit-calc-MOB-1000.json", 1000, 5.99)]
    [InlineData("worldremit-calc-MOB-500.json", 500, 3.49)]
    public void Calculation_normalises_fee_on_top(string fixture, decimal amount, decimal fee)
    {
        var quote = WorldRemitParser.ParseCalculation(Fixture.Read(fixture), Fixture.GbpBdt, amount, Bkash, Fixture.FetchedAt);

        Assert.Equal("WorldRemit", quote.Provider);
        Assert.Equal(163.8408m, quote.Rate);
        Assert.Equal(fee, quote.Fee);
        Assert.Equal(Math.Round((amount - fee) * 163.8408m, 2), quote.ReceiveAmount);
        Assert.Equal("Mobile wallet (bKash)", quote.DeliveryMethod);
        Assert.Equal("Same day", quote.Speed);
    }

    [Fact]
    public void Card_deposit_uses_correspondent_name()
    {
        var method = new WorldRemitPayOutMethod("PTV", "1727", "Debit Card Deposit", "Within 1 hour");
        var quote = WorldRemitParser.ParseCalculation(Fixture.Read("worldremit-calc-PTV-1000.json"), Fixture.GbpBdt, 1000, method,
            Fixture.FetchedAt);

        Assert.Equal(0m, quote.Fee);
        Assert.Equal(160480.10m, quote.ReceiveAmount);
        Assert.Equal("Debit Card Deposit", quote.DeliveryMethod);
    }

    [Fact]
    public void Calculation_error_throws()
    {
        var ex = Assert.Throws<FormatException>(() => WorldRemitParser.ParseCalculation(
            Fixture.Read("worldremit-calc-error-ae.json"), Fixture.AedBdt, 1000, Bkash, Fixture.FetchedAt));

        Assert.Contains("SERVER_ERROR", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
