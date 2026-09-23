using System.Collections.Concurrent;
using JournalApp.Application.Common.Interfaces;

namespace JournalApp.Application.Tests.TestDoubles;

/// <summary>Dictionary-backed cache without expiration, for testing the caching decorators.</summary>
public sealed class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object?> _items = new();

    public IReadOnlyCollection<string> Keys => _items.Keys.ToList();

    public async Task<T> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        if (_items.TryGetValue(key, out var cached))
        {
            return (T)cached!;
        }

        var value = await factory(cancellationToken);
        _items[key] = value;
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        _items.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
