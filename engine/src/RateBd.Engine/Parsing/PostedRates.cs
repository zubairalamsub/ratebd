using RateBd.Engine.Models;

namespace RateBd.Engine.Parsing;

/// <summary>Shared construction and sanity checks for <see cref="PostedRate"/>s.</summary>
internal static class PostedRates
{
    /// <summary>A rate the source itself dates further back than this is rejected rather than published as current.</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(3);

    public static PostedRate Create(
        string provider,
        Corridor corridor,
        decimal rate,
        string deliveryMethod,
        DateTime fetchedAt,
        string speed = Labels.NotSpecified,
        string? partner = null,
        string? feeNote = null,
        DateTime? publishedAt = null)
    {
        if (rate <= 0)
        {
            throw new FormatException($"Implausible {provider} rate {rate} for {corridor.Id}");
        }
        if (publishedAt is { } published && fetchedAt - published > MaxAge)
        {
            throw new FormatException($"{provider} last updated its {corridor.SendCurrency} rate on {published:yyyy-MM-dd HH:mm} UTC");
        }
        // Some sources carry 13 decimals (e.g. 33.7431272137209); six are more than any rate needs.
        return new PostedRate(provider, corridor.Id, corridor.SendCurrency, Math.Round(rate, 6, MidpointRounding.AwayFromZero),
            deliveryMethod, speed, partner, feeNote, publishedAt, fetchedAt, Stale: false);
    }

    /// <summary>Converts a "send currency per BDT" quotation (e.g. 0.0024945340 KWD) to BDT per send unit.</summary>
    public static decimal Invert(decimal sendPerReceive) =>
        sendPerReceive > 0
            ? Math.Round(1m / sendPerReceive, 4, MidpointRounding.AwayFromZero)
            : throw new FormatException($"Implausible inverse rate {sendPerReceive}");
}
