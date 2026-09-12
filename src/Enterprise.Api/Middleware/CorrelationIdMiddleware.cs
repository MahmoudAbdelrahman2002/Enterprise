using Serilog.Context;

namespace Enterprise.Api.Middleware;

/// <summary>
/// Accepts an inbound <c>X-Correlation-Id</c> (so an upstream gateway/caller can supply one and
/// tie their own logs to ours) or generates one, echoes it back on the response, and pushes it
/// into Serilog's <see cref="LogContext"/> so every log line written anywhere during this
/// request - including from deep inside a MediatR handler - carries the same correlation id
/// without that handler needing to know the concept exists.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
