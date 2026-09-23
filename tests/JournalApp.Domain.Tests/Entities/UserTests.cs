using FluentAssertions;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Exceptions;

namespace JournalApp.Domain.Tests.Entities;

public class UserTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ValidData_ReturnsUserWithExpectedValues()
    {
        // Act
        var user = User.Create("demo", "demo@journal.com", "hashed", Now);

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Username.Should().Be("demo");
        user.Email.Should().Be("demo@journal.com");
        user.PasswordHash.Should().Be("hashed");
        user.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_EmailWithUppercaseAndSpaces_StoresNormalizedEmail()
    {
        // Act
        var user = User.Create("demo", "  Demo@Journal.COM ", "hashed", Now);

        // Assert
        user.Email.Should().Be("demo@journal.com");
    }

    [Fact]
    public void Create_UsernameWithSpaces_StoresTrimmedUsername()
    {
        // Act
        var user = User.Create("  demo ", "demo@journal.com", "hashed", Now);

        // Assert
        user.Username.Should().Be("demo");
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData(null)]
    public void Create_UsernameTooShortOrEmpty_ThrowsDomainValidationException(string? username)
    {
        // Act
        var act = () => User.Create(username!, "demo@journal.com", "hashed", Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(User.Username));
    }

    [Fact]
    public void Create_UsernameLongerThan30Characters_ThrowsDomainValidationException()
    {
        // Act
        var act = () => User.Create(new string('a', 31), "demo@journal.com", "hashed", Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(User.Username));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData(null)]
    public void Create_InvalidEmail_ThrowsDomainValidationException(string? email)
    {
        // Act
        var act = () => User.Create("demo", email!, "hashed", Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(User.Email));
    }

    [Fact]
    public void Create_EmptyPasswordHash_ThrowsDomainValidationException()
    {
        // Act
        var act = () => User.Create("demo", "demo@journal.com", "", Now);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey(nameof(User.PasswordHash));
    }
}
