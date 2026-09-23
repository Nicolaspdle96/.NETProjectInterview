using JournalApp.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JournalApp.Infrastructure.Caching;

/// <summary>
/// In-process <see cref="ICacheService"/> backed by its own <see cref="MemoryCache"/>
/// (kept separate from any shared <c>IMemoryCache</c> so its size limit only applies here).
/// The app runs as a single process; a multi-instance deployment would need a distributed
/// implementation of <see cref="ICacheService"/> instead.
/// </summary>
public sealed class MemoryCacheService : ICacheService, IDisposable
{
    private readonly MemoryCache _cache;
    private readonly TimeSpan _expiration;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IOptions<CacheOptions> options, ILogger<MemoryCacheService> logger)
    {
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = options.Value.SizeLimit });
        _expiration = TimeSpan.FromSeconds(options.Value.ExpirationSeconds);
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(key, out T? cached))
        {
            _logger.LogDebug("Cache hit for {CacheKey}", key);
            return cached!;
        }

        _logger.LogDebug("Cache miss for {CacheKey}", key);
        var value = await factory(cancellationToken);

        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration,
            Size = 1
        });

        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public void Dispose() => _cache.Dispose();
}
