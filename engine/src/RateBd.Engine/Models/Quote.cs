namespace RateBd.Engine.Models;

/// <summary>
/// One provider's price for sending <see cref="SendAmount"/> on a corridor.
/// SendAmount is the total the sender pays (fee included); ReceiveAmount is what the recipient gets.
/// </summary>
public sealed record Quote(
    string Provider,
    string Corridor,
    string SendCurrency,
    decimal SendAmount,
    decimal Rate,
    decimal Fee,
    decimal ReceiveAmount,
    string DeliveryMethod,
    string Speed,
    DateTime FetchedAt,
    bool Stale);
