using System.Net;

namespace RateBd.Engine.Providers;

/// <summary>The cookie jar Skrill's typed client uses, shared so the provider can read the session's XSRF token.</summary>
public sealed class SkrillCookies
{
    public CookieContainer Container { get; } = new();
}
