using System.Text.Json;
using Enterprise.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Enterprise.Infrastructure.Caching;

/// <summary>
/// Talks to <see cref="IConnectionMultiplexer"/> directly rather than through the standard
/// <c>IDistributedCache</c> abstraction, because <see cref="ICacheService.RemoveByPrefixAsync"/>
/// needs Redis's <c>SCAN</c> command (via <c>IServer.Keys</c>) - a capability
/// <c>IDistributedCache</c> deliberately doesn't expose, since not every distributed cache
/// backend supports pattern-based key scanning.
/// </summary>
public sealed class RedisCacheService(IConnectionMultiplexer connectionMultiplexer) : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>((string)value!) : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, expiration ?? DefaultExpiration);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _database.KeyDeleteAsync(key);

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var endpoint in connectionMultiplexer.GetEndPoints())
        {
            var server = connectionMultiplexer.GetServer(endpoint);
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
            }
        }
    }
}
