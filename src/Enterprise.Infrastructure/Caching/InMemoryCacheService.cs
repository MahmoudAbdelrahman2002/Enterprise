using System.Collections.Concurrent;
using Enterprise.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Enterprise.Infrastructure.Caching;

/// <summary>
/// Used automatically when no Redis connection string is configured (e.g. local development
/// without Redis installed), so caching degrades gracefully to a single-instance, in-process
/// cache instead of the application failing to start. <see cref="IMemoryCache"/> has no native
/// key-enumeration API, so a side-index of known keys is maintained purely to support
/// <see cref="RemoveByPrefixAsync"/>.
/// </summary>
public sealed class InMemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);
    private static readonly ConcurrentDictionary<string, byte> TrackedKeys = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(memoryCache.TryGetValue(key, out T? value) ? value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        memoryCache.Set(key, value, expiration ?? DefaultExpiration);
        TrackedKeys.TryAdd(key, 0);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        TrackedKeys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in TrackedKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
        {
            memoryCache.Remove(key);
            TrackedKeys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
