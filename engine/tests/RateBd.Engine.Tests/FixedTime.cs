namespace RateBd.Engine.Tests;

/// <summary>A clock stopped at the given UTC time, for parsers that compare a published time with the fetch time.</summary>
internal sealed class FixedTime(DateTime utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
}
