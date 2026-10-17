namespace RateBd.Engine.Providers;

/// <summary>
/// Thrown when a provider has no accessible public quote source for a corridor (or at all).
/// The runner records this as "not_supported" rather than a failure, and nothing is carried forward.
/// </summary>
public sealed class ProviderNotSupportedException(string reason) : Exception(reason);
