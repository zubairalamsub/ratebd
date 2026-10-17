namespace RateBd.Engine.Models;

/// <summary>
/// A remittance rate an exchange house publishes without a fee or a per-amount quote. Kept apart from
/// <see cref="Quote"/>: with no fee there is no total cost, so it cannot be ranked against fee-inclusive quotes.
/// </summary>
/// <param name="Rate">Receive-currency units per one send-currency unit, as published.</param>
/// <param name="Partner">For an aggregator that lists several exchange houses, the one behind the rate.</param>
/// <param name="FeeNote">Fee wording the source publishes next to the rate (e.g. "Starting from QAR 20"); null when none.</param>
/// <param name="PublishedAt">When the source says the rate was set, if it says.</param>
public sealed record PostedRate(
    string Provider,
    string Corridor,
    string SendCurrency,
    decimal Rate,
    string DeliveryMethod,
    string Speed,
    string? Partner,
    string? FeeNote,
    DateTime? PublishedAt,
    DateTime FetchedAt,
    bool Stale);
