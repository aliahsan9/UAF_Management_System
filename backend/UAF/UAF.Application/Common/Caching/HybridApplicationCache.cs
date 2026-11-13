using Microsoft.Extensions.Caching.Hybrid;
namespace UAF.Application.Common.Caching;
internal sealed class HybridApplicationCache(HybridCache cache) : IApplicationCache
{
    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory,
        IEnumerable<string> tags, TimeSpan expiration, CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync(key, factory, new HybridCacheEntryOptions
        {
            Expiration = expiration, LocalCacheExpiration = TimeSpan.FromMinutes(1)
        }, tags, cancellationToken);
    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        cache.RemoveByTagAsync(tag, cancellationToken);
}
