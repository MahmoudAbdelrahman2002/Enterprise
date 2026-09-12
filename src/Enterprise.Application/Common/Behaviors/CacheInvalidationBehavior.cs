using Enterprise.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Common.Behaviors;

/// <summary>
/// Mirror image of <see cref="CachingBehavior{TRequest,TResponse}"/>: runs the command first,
/// and only if it completes without throwing does it purge the cache prefixes the command
/// declared via <see cref="ICacheInvalidatorCommand"/>. A failed write must never invalidate a
/// cache entry that still correctly reflects the (unchanged) database state.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (request is ICacheInvalidatorCommand invalidator)
        {
            foreach (var prefix in invalidator.CacheKeyPrefixesToInvalidate)
            {
                logger.LogDebug("Invalidating cache entries with prefix {Prefix}", prefix);
                await cacheService.RemoveByPrefixAsync(prefix, cancellationToken);
            }
        }

        return response;
    }
}
