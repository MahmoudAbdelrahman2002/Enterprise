using System.Threading.RateLimiting;
using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Microsoft.AspNetCore.RateLimiting;

namespace Enterprise.Api.Extensions;

/// <summary>
/// Uses ASP.NET Core's built-in rate limiting middleware (no third-party package needed since
/// .NET 7) with two policies: a generous global default per client IP, and a much stricter
/// policy applied only to the authentication endpoints - login/refresh are the endpoints an
/// attacker actually wants to brute-force, so they get their own tighter budget rather than
/// sharing the same allowance as read-heavy product browsing.
/// </summary>
public static class RateLimitingExtensions
{
    public const string GlobalPolicy = "global";
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddRateLimitingSetup(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var localizer = context.HttpContext.RequestServices.GetRequiredService<IAppLocalizer>();
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse<object?>.Fail(
                        StatusCodes.Status429TooManyRequests,
                        localizer[MessageKeys.Error.RateLimited],
                        traceId: context.HttpContext.TraceIdentifier),
                    cancellationToken);
            };

            options.AddPolicy(GlobalPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static string GetClientKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
