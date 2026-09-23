using FluentAssertions;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Enums;
using JournalApp.Domain.Exceptions;

namespace JournalApp.Domain.Tests.Entities;

public class JournalEntryTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ValidData_ReturnsEntryWithExpectedValues()
    {
        // Act
        var entry = JournalEntry.Create(UserId, "My day", "It was a good day.", Mood.Good, Now);

        // Assert
        entry.Id.Should().NotBeEmpty();
        entry.UserId.Should().Be(UserId);
        entry.Title.Should().Be("My day");
        entry.Content.Should().Be("It was a good day.");
        entry.Mood.Should().Be(Mood.Good);
        entry.CreatedAt.Should().Be(Now);
        entry.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TitleWithSurroundingSpaces_StoresTrimmedTitle()
    {
        // Act
        var entry = JournalEntry.Create(UserId, "   My day  ", "Content", null, Now);

        // Assert
        entry.Title.Should().Be("My day");
    }

    [Fact]
    public void Create_NullMood_IsAllowed()
    {
        // Act
        var entry = JournalEntry.Create(UserId, "Title", "Content", null, Now);

        // Assert
        entry.Mood.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyTitle_ThrowsDomainValidationException(string? title)
    {
        // Act
        var act = () => JournalEntry.Create(UserId, title!, "Content", null, Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(JournalEntry.Title));
    }

    [Fact]
    public void Create_TitleLongerThan100Characters_ThrowsDomainValidationException()
    {
        // Act
        var act = () => JournalEntry.Create(UserId, new string('a', 101), "Content", null, Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(JournalEntry.Title));
    }

    [Fact]
    public void Create_TitleOf100CharactersAfterTrim_IsValid()
    {
        // Act
        var entry = JournalEntry.Create(UserId, "  " + new string('a', 100) + "  ", "Content", null, Now);

        // Assert
        entry.Title.Should().HaveLength(100);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyContent_ThrowsDomainValidationException(string? content)
    {
        // Act
        var act = () => JournalEntry.Create(UserId, "Title", content!, null, Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(JournalEntry.Content));
    }

    [Fact]
    public void Create_ContentLongerThan5000Characters_ThrowsDomainValidationException()
    {
        // Act
        var act = () => JournalEntry.Create(UserId, "Title", new string('a', 5001), null, Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(JournalEntry.Content));
    }

    [Fact]
    public void Create_InvalidMoodValue_ThrowsDomainValidationException()
    {
        // Act
        var act = () => JournalEntry.Create(UserId, "Title", "Content", (Mood)42, Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(JournalEntry.Mood));
    }

    [Fact]
    public void Create_EmptyUserId_ThrowsDomainValidationException()
    {
        // Act
        var act = () => JournalEntry.Create(Guid.Empty, "Title", "Content", null, Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(JournalEntry.UserId));
    }

    [Fact]
    public void Update_ValidData_ChangesValuesAndSetsUpdatedAt()
    {
        // Arrange
        var entry = JournalEntry.Create(UserId, "Title", "Content", Mood.Bad, Now);
        var later = Now.AddHours(2);

        // Act
        entry.Update("  New title ", "New content", Mood.Great, later);

        // Assert
        entry.Title.Should().Be("New title");
        entry.Content.Should().Be("New content");
        entry.Mood.Should().Be(Mood.Great);
        entry.UpdatedAt.Should().Be(later);
        entry.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Update_EmptyTitle_ThrowsAndKeepsOriginalValues()
    {
        // Arrange
        var entry = JournalEntry.Create(UserId, "Title", "Content", Mood.Bad, Now);

        // Act
        var act = () => entry.Update("", "New content", Mood.Great, Now.AddHours(1));

        // Assert
        act.Should().Throw<DomainValidationException>();
        entry.Title.Should().Be("Title");
        entry.Content.Should().Be("Content");
        entry.Mood.Should().Be(Mood.Bad);
        entry.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Update_ContentTooLong_ThrowsDomainValidationException()
    {
        // Arrange
        var entry = JournalEntry.Create(UserId, "Title", "Content", null, Now);

        // Act
        var act = () => entry.Update("Title", new string('a', 5001), null, Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(JournalEntry.Content));
    }

    [Fact]
    public void IsOwnedBy_Owner_ReturnsTrue()
    {
        // Arrange
        var entry = JournalEntry.Create(UserId, "Title", "Content", null, Now);

        // Act
        var result = entry.IsOwnedBy(UserId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsOwnedBy_OtherUser_ReturnsFalse()
    {
        // Arrange
        var entry = JournalEntry.Create(UserId, "Title", "Content", null, Now);

        // Act
        var result = entry.IsOwnedBy(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }
}
