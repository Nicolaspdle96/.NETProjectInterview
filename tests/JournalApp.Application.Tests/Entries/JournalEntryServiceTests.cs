using FluentAssertions;
using JournalApp.Application.Common.Interfaces;
using JournalApp.Application.Entries;
using JournalApp.Application.Entries.Dtos;
using JournalApp.Application.Entries.Validators;
using JournalApp.Application.Tests.TestDoubles;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Enums;
using JournalApp.Domain.Exceptions;
using Moq;

namespace JournalApp.Application.Tests.Entries;

public class JournalEntryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Mock<IJournalEntryRepository> _entries = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FixedTimeProvider _time = new(Now);
    private readonly JournalEntryService _sut;

    public JournalEntryServiceTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(_currentUserId);
        _sut = new JournalEntryService(
            _entries.Object,
            _currentUser.Object,
            _unitOfWork.Object,
            new CreateEntryRequestValidator(),
            new UpdateEntryRequestValidator(),
            _time);
    }

    private JournalEntry OwnEntry(string title = "Title") =>
        JournalEntry.Create(_currentUserId, title, "Content", Mood.Good, Now.UtcDateTime);

    private static JournalEntry ForeignEntry() =>
        JournalEntry.Create(Guid.NewGuid(), "Someone else's", "Private", Mood.Bad, Now.UtcDateTime);

    [Fact]
    public async Task ListAsync_CurrentUser_ReturnsOnlyEntriesFromRepositoryForThatUser()
    {
        // Arrange
        var entries = new List<JournalEntry> { OwnEntry("Newest"), OwnEntry("Oldest") };
        _entries.Setup(r => r.ListByUserAsync(_currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // Act
        var result = await _sut.ListAsync(CancellationToken.None);

        // Assert
        result.Select(e => e.Title).Should().Equal("Newest", "Oldest");
        _entries.Verify(r => r.ListByUserAsync(_currentUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_NoAuthenticatedUser_ThrowsUnauthorizedException()
    {
        // Arrange
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        // Act
        var act = () => _sut.ListAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task GetByIdAsync_OwnEntry_ReturnsDto()
    {
        // Arrange
        var entry = OwnEntry();
        _entries.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        // Act
        var result = await _sut.GetByIdAsync(entry.Id, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new EntryDto(entry.Id, "Title", "Content", Mood.Good, Now.UtcDateTime, null));
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntry_ThrowsNotFoundException()
    {
        // Arrange
        _entries.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JournalEntry?)null);

        // Act
        var act = () => _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_EntryOfAnotherUser_ThrowsNotFoundException()
    {
        // Arrange
        var entry = ForeignEntry();
        _entries.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        // Act
        var act = () => _sut.GetByIdAsync(entry.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsEntryForCurrentUserWithServerTimestamp()
    {
        // Arrange
        JournalEntry? added = null;
        _entries.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((entry, _) => added = entry);

        // Act
        var result = await _sut.CreateAsync(new CreateEntryRequest("  My day ", "Content", Mood.Great), CancellationToken.None);

        // Assert
        added.Should().NotBeNull();
        added!.UserId.Should().Be(_currentUserId);
        added.CreatedAt.Should().Be(Now.UtcDateTime);
        result.Id.Should().Be(added.Id);
        result.Title.Should().Be("My day");
        result.Mood.Should().Be(Mood.Great);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ThrowsValidationException()
    {
        // Act
        var act = () => _sut.CreateAsync(new CreateEntryRequest("", "Content", null), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<DomainValidationException>())
            .Which.Errors.Should().ContainKey("Title");
        _entries.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OwnEntry_UpdatesValuesAndSetsUpdatedAt()
    {
        // Arrange
        var entry = OwnEntry();
        _entries.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);
        _time.Now = Now.AddHours(3);

        // Act
        var result = await _sut.UpdateAsync(entry.Id, new UpdateEntryRequest("New", "New content", null), CancellationToken.None);

        // Assert
        result.Title.Should().Be("New");
        result.Content.Should().Be("New content");
        result.Mood.Should().BeNull();
        result.UpdatedAt.Should().Be(Now.AddHours(3).UtcDateTime);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_EntryOfAnotherUser_ThrowsNotFoundExceptionAndDoesNotModify()
    {
        // Arrange
        var entry = ForeignEntry();
        _entries.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        // Act
        var act = () => _sut.UpdateAsync(entry.Id, new UpdateEntryRequest("Hacked", "Hacked", null), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        entry.Title.Should().Be("Someone else's");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var entry = OwnEntry();
        _entries.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        // Act
        var act = () => _sut.UpdateAsync(entry.Id, new UpdateEntryRequest("Title", new string('a', 5001), null), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task DeleteAsync_OwnEntry_RemovesAndSaves()
    {
        // Arrange
        var entry = OwnEntry();
        _entries.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        // Act
        await _sut.DeleteAsync(entry.Id, CancellationToken.None);

        // Assert
        _entries.Verify(r => r.Remove(entry), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_EntryOfAnotherUser_ThrowsNotFoundException()
    {
        // Arrange
        var entry = ForeignEntry();
        _entries.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        // Act
        var act = () => _sut.DeleteAsync(entry.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _entries.Verify(r => r.Remove(It.IsAny<JournalEntry>()), Times.Never);
    }
}
