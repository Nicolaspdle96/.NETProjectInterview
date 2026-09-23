using FluentAssertions;
using JournalApp.Application.Common.Interfaces;
using JournalApp.Application.Entries;
using JournalApp.Application.Entries.Dtos;
using JournalApp.Application.Tests.TestDoubles;
using JournalApp.Domain.Enums;
using JournalApp.Domain.Exceptions;
using Moq;

namespace JournalApp.Application.Tests.Entries;

public class CachedJournalEntryServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IJournalEntryService> _inner = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly CachedJournalEntryService _sut;

    public CachedJournalEntryServiceTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(() => _userId);
        _inner.Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [Entry("From database")]);
        _inner.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Entry("From database", id));
        _sut = new CachedJournalEntryService(_inner.Object, _cache, _currentUser.Object);
    }

    private static EntryDto Entry(string title, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), title, "Content", Mood.Good, Now, null);

    [Fact]
    public async Task ListAsync_CalledTwice_QueriesInnerServiceOnce()
    {
        // Act
        var first = await _sut.ListAsync(CancellationToken.None);
        var second = await _sut.ListAsync(CancellationToken.None);

        // Assert
        second.Should().BeEquivalentTo(first);
        _inner.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_DifferentUsers_CachesEachUserSeparately()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var currentUserId = _userId;
        _currentUser.Setup(c => c.UserId).Returns(() => currentUserId);
        await _sut.ListAsync(CancellationToken.None);

        // Act
        currentUserId = otherUserId;
        await _sut.ListAsync(CancellationToken.None);

        // Assert
        _inner.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ListAsync_NoAuthenticatedUser_ThrowsUnauthorizedExceptionWithoutCaching()
    {
        // Arrange
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        // Act
        var act = () => _sut.ListAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        _inner.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cache.Keys.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_CalledTwice_QueriesInnerServiceOnce()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        await _sut.GetByIdAsync(id, CancellationToken.None);
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Id.Should().Be(id);
        _inner.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_DoesNotCacheTheFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _inner.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("not found"));

        // Act
        var first = () => _sut.GetByIdAsync(id, CancellationToken.None);
        var second = () => _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        await first.Should().ThrowAsync<NotFoundException>();
        await second.Should().ThrowAsync<NotFoundException>();
        _inner.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByIdAsync_SameEntryIdForAnotherUser_DoesNotServeCachedEntry()
    {
        // Arrange
        var id = Guid.NewGuid();
        var currentUserId = _userId;
        _currentUser.Setup(c => c.UserId).Returns(() => currentUserId);
        await _sut.GetByIdAsync(id, CancellationToken.None);
        currentUserId = Guid.NewGuid();
        _inner.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("not found"));

        // Act
        var act = () => _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_AfterListWasCached_NextListQueriesInnerServiceAgain()
    {
        // Arrange
        await _sut.ListAsync(CancellationToken.None);
        var request = new CreateEntryRequest("New", "Content", null);
        _inner.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(Entry("New"));

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);
        await _sut.ListAsync(CancellationToken.None);

        // Assert
        _inner.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_AfterReadsWereCached_NextReadsQueryInnerServiceAgain()
    {
        // Arrange
        var id = Guid.NewGuid();
        await _sut.ListAsync(CancellationToken.None);
        await _sut.GetByIdAsync(id, CancellationToken.None);
        var request = new UpdateEntryRequest("Updated", "Content", null);
        _inner.Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>())).ReturnsAsync(Entry("Updated", id));

        // Act
        await _sut.UpdateAsync(id, request, CancellationToken.None);
        await _sut.ListAsync(CancellationToken.None);
        await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        _inner.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _inner.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_AfterReadsWereCached_NextReadsQueryInnerServiceAgain()
    {
        // Arrange
        var id = Guid.NewGuid();
        await _sut.ListAsync(CancellationToken.None);
        await _sut.GetByIdAsync(id, CancellationToken.None);

        // Act
        await _sut.DeleteAsync(id, CancellationToken.None);
        await _sut.ListAsync(CancellationToken.None);
        await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        _inner.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _inner.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _inner.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_ByAnotherUser_KeepsCurrentUsersCache()
    {
        // Arrange
        var currentUserId = _userId;
        _currentUser.Setup(c => c.UserId).Returns(() => currentUserId);
        await _sut.ListAsync(CancellationToken.None);
        var request = new CreateEntryRequest("Other", "Content", null);
        _inner.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(Entry("Other"));

        // Act
        currentUserId = Guid.NewGuid();
        await _sut.CreateAsync(request, CancellationToken.None);
        currentUserId = _userId;
        await _sut.ListAsync(CancellationToken.None);

        // Assert
        _inner.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InnerServiceFails_StillInvalidatesAndRethrows()
    {
        // Arrange
        var id = Guid.NewGuid();
        await _sut.ListAsync(CancellationToken.None);
        var request = new UpdateEntryRequest("", "Content", null);
        _inner.Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainValidationException("Title", "Title is required."));

        // Act
        var act = () => _sut.UpdateAsync(id, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>();
        await _sut.ListAsync(CancellationToken.None);
        _inner.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
