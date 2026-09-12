using Hangfire.Dashboard;

namespace Enterprise.Api.Extensions;

/// <summary>
/// The Hangfire dashboard is a browser UI, not a JSON API endpoint - it can't easily be gated
/// by the same JWT/API-key bearer schemes the rest of this template uses, since a browser
/// navigating to "/hangfire" doesn't attach an Authorization header. Restricting it to
/// localhost is the standard baseline for this kind of embedded operational dashboard; a real
/// deployment should put it behind a VPN/reverse-proxy IP allowlist or a cookie-based admin
/// login, which is documented in SECURITY.md rather than implemented here to avoid dragging a
/// second authentication system into the template.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.Connection.RemoteIpAddress is not null &&
               System.Net.IPAddress.IsLoopback(httpContext.Connection.RemoteIpAddress);
    }
}
