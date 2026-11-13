namespace UAF.Application.Common.Caching;
public interface IApplicationCache
{
    ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory,
        IEnumerable<string> tags, TimeSpan expiration, CancellationToken cancellationToken = default);
    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
