namespace Enterprise.Api.Middleware;

/// <summary>
/// Headers that matter for a JSON API specifically (as opposed to a server-rendered app):
///   - X-Content-Type-Options prevents a browser from MIME-sniffing a JSON response into
///     something executable (part of this template's XSS mitigation posture - see SECURITY.md).
///   - X-Frame-Options/frame-ancestors stop the API's responses from being framed at all.
///   - A restrictive default-src 'none' CSP is applied to JSON API responses only. Swagger UI
///     and Hangfire are HTML surfaces; CSP is omitted there so their scripts/styles can load.
/// HSTS itself is handled by the built-in <c>app.UseHsts()</c> middleware, not duplicated here.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string ApiCsp = "default-src 'none'; frame-ancestors 'none'";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            var path = context.Request.Path.Value ?? string.Empty;
            // Swagger UI and Hangfire are HTML/JS apps. Applying CSP (even a relaxed one) to
            // them has blanked the page in practice - skip CSP entirely for those surfaces and
            // keep the strict policy on JSON API responses only.
            var isHtmlUi = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                           || path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase)
                           || path.EndsWith(".html", StringComparison.OrdinalIgnoreCase);

            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
            headers.Remove("Server");

            if (!isHtmlUi)
            {
                headers["X-Frame-Options"] = "DENY";
                headers["Content-Security-Policy"] = ApiCsp;
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
