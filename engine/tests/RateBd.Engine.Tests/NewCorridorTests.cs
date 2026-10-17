using Microsoft.Extensions.Logging.Abstractions;
using RateBd.Engine.Parsing;
using RateBd.Engine.Providers;

namespace RateBd.Engine.Tests;

/// <summary>USD, EUR (Italy), MYR and SGD corridors, from responses captured on 2026-10-17.</summary>
public class NewCorridorTests
{
    // ---------------------------------------------------------------- Wise

    [Theory]
    [InlineData("wise-usd-bdt-500.json", "usd", 500, 123.0, 11.79, 60049.83, "Within 9 days")]
    [InlineData("wise-usd-bdt-1000.json", "usd", 1000, 123.0, 15.17, 121134.09, "Within 9 days")]
    [InlineData("wise-eur-bdt-500.json", "eur", 500, 138.234, 6.42, 68229.54, "Within 2 days")]
    [InlineData("wise-myr-bdt-1000.json", "myr", 1000, 30.1206, 21.01, 29487.77, "Within 2 days")]
    [InlineData("wise-sgd-bdt-500.json", "sgd", 500, 96.12, 5.69, 47513.08, "Within 2 days")]
    public void Wise_uses_bank_transfer_for_new_currencies(string fixture, string from, decimal amount, decimal rate,
        decimal fee, decimal receive, string speed)
    {
        var corridor = from switch { "usd" => Fixture.UsdBdt, "eur" => Fixture.EurBdt, "myr" => Fixture.MyrBdt, _ => Fixture.SgdBdt };

        var quote = WiseParser.Parse(Fixture.Read(fixture), corridor, amount, Fixture.FetchedAt);

        Assert.Equal(rate, quote.Rate);
        Assert.Equal(fee, quote.Fee);
        Assert.Equal(receive, quote.ReceiveAmount);
        Assert.Equal(speed, quote.Speed);
        Assert.Equal("Bank deposit", quote.DeliveryMethod);
    }

    [Fact]
    public void Wise_route_not_supported_is_not_a_failure()
    {
        var ex = Assert.Throws<ProviderNotSupportedException>(() =>
            WiseParser.ThrowIfRouteNotSupported(Fixture.Read("wise-sar-bdt-422.json"), Fixture.SarBdt));

        Assert.Contains("SAR→BDT", ex.Message);
    }

    [Fact]
    public void Wise_ignores_unrelated_error_bodies()
    {
        WiseParser.ThrowIfRouteNotSupported("""{"errors":[{"code":"error.other"}]}""", Fixture.SarBdt);
        WiseParser.ThrowIfRouteNotSupported("<html>gateway error</html>", Fixture.SarBdt);
    }

    [Fact]
    public async Task Wise_provider_maps_422_to_not_supported()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent(Fixture.Read("wise-sar-bdt-422.json")),
        });
        var provider = new WiseProvider(new HttpClient(handler), TimeProvider.System);

        await Assert.ThrowsAsync<ProviderNotSupportedException>(() =>
            provider.GetQuotesAsync(Fixture.SarBdt, [1000m], CancellationToken.None));
    }

    // ---------------------------------------------------------------- Remitly

    [Theory]
    [InlineData("remitly-usd-bdt-500.json", "usd", 122.79)]
    [InlineData("remitly-eur-bdt-500.json", "eur", 137.26)]
    public void Remitly_prefers_bank_deposit_over_the_cash_pickup_headline(string fixture, string from, decimal rate)
    {
        var corridor = from == "usd" ? Fixture.UsdBdt : Fixture.EurBdt;

        var quote = RemitlyParser.Parse(Fixture.Read(fixture), corridor, 500, Fixture.FetchedAt);

        Assert.Equal("Bank deposit", quote.DeliveryMethod);
        Assert.Equal(0.99m, quote.Fee); // cash pickup headline is 3.99 USD / 2.99 EUR
        Assert.Equal(rate, quote.Rate);
        Assert.Equal(Math.Round((500m - 0.99m) * rate, 2), quote.ReceiveAmount);
    }

    [Fact]
    public void Remitly_falls_back_to_headline_estimate_without_alternatives()
    {
        var quote = RemitlyParser.Parse(Fixture.Read("remitly-sgd-bdt-500.json"), Fixture.SgdBdt, 500, Fixture.FetchedAt);

        Assert.Equal(94.48m, quote.Rate);
        Assert.Equal(4.99m, quote.Fee);
        Assert.Equal("Not specified", quote.DeliveryMethod);
    }

    [Fact]
    public void Remitly_unsupported_corridor_body_is_not_supported()
    {
        Assert.Throws<ProviderNotSupportedException>(() =>
            RemitlyParser.ThrowIfUnsupportedCorridor(Fixture.Read("remitly-sar-bdt-400.json"), Fixture.SarBdt));
        RemitlyParser.ThrowIfUnsupportedCorridor(Fixture.Read("remitly-429.json"), Fixture.SarBdt); // different error key
    }

    [Fact]
    public async Task Remitly_unmapped_send_country_makes_no_request()
    {
        var handler = new FakeHandler(_ => throw new InvalidOperationException("no request expected"));
        var provider = new RemitlyProvider(new HttpClient(handler), TimeProvider.System, NullLogger<RemitlyProvider>.Instance);

        await Assert.ThrowsAsync<ProviderNotSupportedException>(() =>
            provider.GetQuotesAsync(Fixture.MyrBdt, [1000m], CancellationToken.None));
        Assert.Empty(handler.Requests);
    }

    // ---------------------------------------------------------------- Taptap Send

    [Theory]
    [InlineData("usd", 122.55)]
    [InlineData("eur", 138.25)]
    public void Taptap_serves_usa_and_italy(string from, decimal rate)
    {
        var corridor = from == "usd" ? Fixture.UsdBdt : Fixture.EurBdt;

        var quotes = TaptapSendParser.Parse(Fixture.Read("taptapsend-fxrates.json"), corridor, [500m, 1000m], Fixture.FetchedAt);

        Assert.All(quotes, q => Assert.Equal(rate, q.Rate));
        Assert.All(quotes, q => Assert.Equal(0m, q.Fee));
    }

    [Fact]
    public void Taptap_percentage_fee_schedule_is_capped()
    {
        // US→VN (USD payout) in the captured feed: 1% with flatFee 0 and maxFee 100.
        var usdVn = new RateBd.Engine.Models.Corridor("usd-usd-vn", "USD", "USD", "US", "VN");
        var quotes = TaptapSendParser.Parse(Fixture.Read("taptapsend-fxrates.json"), usdVn, [500m, 20000m], Fixture.FetchedAt);

        Assert.Equal(5.00m, quotes[0].Fee);
        Assert.Equal(100m, quotes[1].Fee);
    }

    [Fact]
    public void Taptap_does_not_serve_singapore()
    {
        Assert.Throws<ProviderNotSupportedException>(() =>
            TaptapSendParser.Parse(Fixture.Read("taptapsend-fxrates.json"), Fixture.SgdBdt, [500m], Fixture.FetchedAt));
    }

    // ---------------------------------------------------------------- WorldRemit

    [Fact]
    public void WorldRemit_usa_offers_bkash_and_quotes_it()
    {
        var methods = WorldRemitParser.ParsePayOutMethods(Fixture.Read("worldremit-payoutmethods-us-bd.json"));
        var mob = Assert.Single(methods);

        var quote = WorldRemitParser.ParseCalculation(Fixture.Read("worldremit-calc-usd-MOB-500.json"), Fixture.UsdBdt, 500, mob,
            Fixture.FetchedAt);

        Assert.Equal(122.939m, quote.Rate);
        Assert.Equal(3.49m, quote.Fee);
        Assert.Equal(61040.44m, quote.ReceiveAmount); // (500 - 3.49) * 122.939
        Assert.Equal("Mobile wallet (bKash)", quote.DeliveryMethod);
    }

    [Fact]
    public void WorldRemit_integer_fee_parses()
    {
        var mob = new WorldRemitPayOutMethod("MOB", "4004", "bKash Mobile Money", "Same day");

        var quote = WorldRemitParser.ParseCalculation(Fixture.Read("worldremit-calc-myr-MOB-1000.json"), Fixture.MyrBdt, 1000, mob,
            Fixture.FetchedAt);

        Assert.Equal(5m, quote.Fee);
        Assert.Equal(29964.43m, quote.ReceiveAmount); // (1000 - 5) * 30.115
    }

    [Fact]
    public void WorldRemit_airtime_only_country_has_no_money_transfer_methods()
    {
        Assert.Empty(WorldRemitParser.ParsePayOutMethods(Fixture.Read("worldremit-payoutmethods-sa-bd.json")));
    }

    // ---------------------------------------------------------------- Xe

    [Theory]
    [InlineData("xe-usd-bdt-500.json", "usd", 500, 122.8484, 4.00, "Typically 3 business days")]
    [InlineData("xe-usd-bdt-1000.json", "usd", 1000, 122.8484, 0.00, "Typically 3 business days")]
    [InlineData("xe-eur-bdt-500.json", "eur", 500, 137.8581, 4.00, "Typically within 48 hours")]
    public void Xe_usa_and_italy_use_default_option(string fixture, string from, decimal amount, decimal rate, decimal fee,
        string speed)
    {
        var corridor = from == "usd" ? Fixture.UsdBdt : Fixture.EurBdt;

        var quote = XeParser.Parse(Fixture.Read(fixture), corridor, amount, Fixture.FetchedAt);

        Assert.Equal(rate, quote.Rate);
        Assert.Equal(fee, quote.Fee);
        Assert.Equal(Math.Round((amount - fee) * rate, 2), quote.ReceiveAmount);
        Assert.Equal(speed, quote.Speed);
        Assert.Equal("Bank deposit", quote.DeliveryMethod);
    }

    [Fact]
    public void Xe_quote006_is_not_supported()
    {
        var ex = Assert.Throws<ProviderNotSupportedException>(() =>
            XeParser.ThrowIfCurrencyNotAvailable(Fixture.Read("xe-sar-bdt-400.json"), Fixture.SarBdt));

        Assert.Contains("QUOTE006", ex.Message);
    }

    [Fact]
    public async Task Xe_rejects_unsupported_send_country_without_a_request()
    {
        var handler = new FakeHandler(_ => throw new InvalidOperationException("no request expected"));
        var provider = new XeProvider(new HttpClient(handler), TimeProvider.System);

        await Assert.ThrowsAsync<ProviderNotSupportedException>(() =>
            provider.GetQuotesAsync(Fixture.SgdBdt, [500m], CancellationToken.None));
        Assert.Empty(handler.Requests);
    }

    // ---------------------------------------------------------------- MidMarket

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
