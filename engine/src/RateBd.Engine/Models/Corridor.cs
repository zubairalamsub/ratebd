namespace RateBd.Engine.Models;

/// <param name="Id">Lower-case slug used in file names, e.g. "gbp-bdt".</param>
/// <param name="SendCountry">ISO 3166-1 alpha-2 code of the sending country, e.g. "GB".</param>
/// <param name="ReceiveCountry">ISO 3166-1 alpha-2 code of the receiving country, e.g. "BD".</param>
public sealed record Corridor(string Id, string SendCurrency, string ReceiveCurrency, string SendCountry, string ReceiveCountry);
