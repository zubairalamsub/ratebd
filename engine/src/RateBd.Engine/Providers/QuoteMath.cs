namespace RateBd.Engine.Providers;

internal static class QuoteMath
{
    /// <summary>
    /// Normalises a quote to "the sender pays <paramref name="sendAmount"/> in total":
    /// the fee comes out of the send amount and the remainder is converted at <paramref name="rate"/>.
    /// Used for providers that quote the fee on top of the amount converted.
    /// </summary>
    public static decimal ReceiveFor(decimal sendAmount, decimal fee, decimal rate) =>
        Math.Round((sendAmount - fee) * rate, 2, MidpointRounding.AwayFromZero);

    public const string NotSpecified = "Not specified";
}
