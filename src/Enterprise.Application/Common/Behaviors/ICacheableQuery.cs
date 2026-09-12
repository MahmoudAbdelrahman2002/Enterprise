namespace Enterprise.Application.Common.Behaviors;

/// <summary>
/// Opt-in marker for queries: implementing this is the entire integration surface for caching.
/// <see cref="CachingBehavior{TRequest,TResponse}"/> looks for it in the MediatR pipeline so
/// individual query handlers never call <c>ICacheService</c> themselves.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
}
