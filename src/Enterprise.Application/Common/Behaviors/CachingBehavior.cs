using Enterprise.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Common.Behaviors;

/// <summary>
/// The entire caching feature, as a single pipeline step: if <typeparamref name="TRequest"/>
/// implements <see cref="ICacheableQuery"/>, try the cache first and short-circuit on a hit;
/// otherwise fall through to the handler and populate the cache with its result. Every other
/// query is completely unaffected - no base class, no opt-out attribute needed.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next(cancellationToken);
        }

        var cached = await cacheService.GetAsync<TResponse>(cacheableQuery.CacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for {CacheKey}", cacheableQuery.CacheKey);
            return cached;
        }

        logger.LogDebug("Cache miss for {CacheKey}", cacheableQuery.CacheKey);
        var response = await next(cancellationToken);

        await cacheService.SetAsync(cacheableQuery.CacheKey, response, cacheableQuery.Expiration, cancellationToken);

        return response;
    }
}
