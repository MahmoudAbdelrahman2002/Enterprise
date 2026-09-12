using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Common.Behaviors;

/// <summary>
/// Structured, request-scoped logging + a slow-request warning, combined into a single
/// behavior rather than two separate "Logging" and "Performance" behaviors - both concerns
/// need the same start/stop timing, so splitting them would just mean measuring elapsed time
/// twice.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
            {
                logger.LogWarning(
                    "Slow request: {RequestName} took {ElapsedMilliseconds}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch
        {
            stopwatch.Stop();
            logger.LogWarning(
                "{RequestName} failed after {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
