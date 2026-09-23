namespace JournalApp.Application.Common.Interfaces;

/// <summary>
/// Application-level cache. Implementations decide storage and expiration.
/// Only immutable values (DTOs) should be cached, since instances are shared between requests.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, or runs <paramref name="factory"/>,
    /// caches its result and returns it. Exceptions from the factory are not cached.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
