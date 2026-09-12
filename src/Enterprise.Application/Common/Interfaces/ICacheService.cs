namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// One abstraction over a two-tier cache (Redis as the shared L2 cache across instances,
/// <c>IMemoryCache</c> as an in-process L1/fallback so a cold Redis connection degrades to
/// "no cache" instead of throwing). Consumed almost exclusively by
/// <c>CachingBehavior&lt;TRequest,TResponse&gt;</c>, not called ad hoc from individual handlers.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
