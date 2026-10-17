using RateBd.Engine.Parsing;

namespace RateBd.Engine.Tests;

public class RemitlyParserTests
{
    [Theory]
    [InlineData("remitly-gbp-bdt-500.json", 500)]
    [InlineData("remitly-gbp-bdt-1000.json", 1000)]
    public void Gbp_uses_standard_rate_and_undiscounted_fee(string fixture, decimal amount)
    {
        var quote = RemitlyParser.Parse(Fixture.Read(fixture), Fixture.GbpBdt, amount, Fixture.FetchedAt);

        Assert.Equal("Remitly", quote.Provider);
        Assert.Equal(163.39m, quote.Rate);   // base_rate, not the 165.84 promotional rate
        Assert.Equal(0.99m, quote.Fee);      // total_fee_amount, even though the promo discounts it to zero
        Assert.Equal(Math.Round((amount - 0.99m) * 163.39m, 2), quote.ReceiveAmount);
        Assert.Equal("Bank deposit", quote.DeliveryMethod);
        Assert.Equal("Not specified", quote.Speed);
    }

    [Fact]
    public void Aed_with_empty_payout_method_is_not_specified()
    {
        var quote = RemitlyParser.Parse(Fixture.Read("remitly-aed-bdt-1000.json"), Fixture.AedBdt, 1000, Fixture.FetchedAt);

        Assert.Equal(33.54m, quote.Rate);
        Assert.Equal(9.00m, quote.Fee);
        Assert.Equal(33238.14m, quote.ReceiveAmount); // (1000 - 9) * 33.54
        Assert.Equal("Not specified", quote.DeliveryMethod);
        Assert.Equal("AED", quote.SendCurrency);
    }

    [Fact]
    public void Rate_limit_error_body_throws()
    {
        var ex = Assert.Throws<FormatException>(() =>
            RemitlyParser.Parse(Fixture.Read("remitly-429.json"), Fixture.GbpBdt, 500, Fixture.FetchedAt));

        Assert.Contains("NOT_ALLOWED", ex.Message);
    }
}

public class RemitlyDeviceIdTests
{
    [Fact]
    public void Device_id_is_read_from_the_page_context()
    {
        // Real page excerpt with the ID value redacted (see the fixture's header comment).
        var id = RemitlyParser.ParseDeviceEnvironmentId(Fixture.Read("remitly-converter-page-excerpt.html"), []);

        Assert.Equal("REDACTEDDEVICEENVIRONMENTIDFORTESTS0123456789", id);
    }

    [Fact]
    public void Falls_back_to_the_de_id_cookie()
    {
        var id = RemitlyParser.ParseDeviceEnvironmentId("<html></html>",
            ["ci_session=abc; Path=/", "de_id=AbCdEfGhIjKlMnOpQrStUv_-12; Max-Age=315360000; Path=/; Secure"]);

        Assert.Equal("AbCdEfGhIjKlMnOpQrStUv_-12", id);
    }

    [Fact]
    public void Returns_null_when_absent()
    {
        Assert.Null(RemitlyParser.ParseDeviceEnvironmentId("<html>no context</html>", ["other=1"]));
    }
}
