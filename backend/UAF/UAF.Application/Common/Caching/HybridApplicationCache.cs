using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;

namespace UAF.Application.Common.Caching;

/// <summary>
/// Uses HybridCache for distributed coordination and an explicit local memory front-cache.
/// Local entries use both sliding and absolute expiration; tag eviction clears both layers.
/// </summary>
internal sealed class HybridApplicationCache(HybridCache cache, IMemoryCache memoryCache) : IApplicationCache
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _keysByTag = new();

    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        IEnumerable<string> tags,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(key, out T? localValue) && localValue is not null)
            return localValue;

        var tagArray = tags.Distinct(StringComparer.Ordinal).ToArray();
        var value = await cache.GetOrCreateAsync(
            key,
            factory,
            new HybridCacheEntryOptions
            {
                Expiration = expiration,
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            tagArray,
            cancellationToken);

        memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            SlidingExpiration = TimeSpan.FromMinutes(1)
        });

        foreach (var tag in tagArray)
            _keysByTag.GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>())[key] = 0;

        return value;
    }

    public async ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await cache.RemoveByTagAsync(tag, cancellationToken);
        if (_keysByTag.TryRemove(tag, out var keys))
        {
            foreach (var key in keys.Keys)
                memoryCache.Remove(key);
        }
    }
}
