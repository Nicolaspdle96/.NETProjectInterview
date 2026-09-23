using FluentAssertions;
using JournalApp.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JournalApp.Infrastructure.Tests.Caching;

public sealed class MemoryCacheServiceTests : IDisposable
{
    private readonly MemoryCacheService _sut = Create(new CacheOptions { ExpirationSeconds = 60, SizeLimit = 100 });

    private static MemoryCacheService Create(CacheOptions options) =>
        new(Options.Create(options), NullLogger<MemoryCacheService>.Instance);

    public void Dispose() => _sut.Dispose();

    [Fact]
    public async Task GetOrCreateAsync_SameKeyTwice_RunsFactoryOnce()
    {
        // Arrange
        var calls = 0;
        Task<string> Factory(CancellationToken _) => Task.FromResult($"value-{++calls}");

        // Act
        var first = await _sut.GetOrCreateAsync("key", Factory, CancellationToken.None);
        var second = await _sut.GetOrCreateAsync("key", Factory, CancellationToken.None);

        // Assert
        first.Should().Be("value-1");
        second.Should().Be("value-1");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_DifferentKeys_CachesEachValue()
    {
        // Act
        var a = await _sut.GetOrCreateAsync("a", _ => Task.FromResult(1), CancellationToken.None);
        var b = await _sut.GetOrCreateAsync("b", _ => Task.FromResult(2), CancellationToken.None);

        // Assert
        a.Should().Be(1);
        b.Should().Be(2);
    }

    [Fact]
    public async Task GetOrCreateAsync_FactoryThrows_DoesNotCacheAndRethrows()
    {
        // Arrange
        var act = () => _sut.GetOrCreateAsync<string>(
            "failing", _ => throw new InvalidOperationException("boom"), CancellationToken.None);

        // Act
        await act.Should().ThrowAsync<InvalidOperationException>();
        var value = await _sut.GetOrCreateAsync("failing", _ => Task.FromResult("recovered"), CancellationToken.None);

        // Assert
        value.Should().Be("recovered");
    }

    [Fact]
    public async Task RemoveAsync_CachedKey_NextReadRunsFactoryAgain()
    {
        // Arrange
        await _sut.GetOrCreateAsync("key", _ => Task.FromResult("old"), CancellationToken.None);

        // Act
        await _sut.RemoveAsync("key", CancellationToken.None);
        var value = await _sut.GetOrCreateAsync("key", _ => Task.FromResult("new"), CancellationToken.None);

        // Assert
        value.Should().Be("new");
    }

    [Fact]
    public async Task GetOrCreateAsync_AfterExpiration_RunsFactoryAgain()
    {
        // Arrange
        using var sut = Create(new CacheOptions { ExpirationSeconds = 1, SizeLimit = 100 });
        await sut.GetOrCreateAsync("key", _ => Task.FromResult("old"), CancellationToken.None);

        // Act
        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        var value = await sut.GetOrCreateAsync("key", _ => Task.FromResult("new"), CancellationToken.None);

        // Assert
        value.Should().Be("new");
    }
}
