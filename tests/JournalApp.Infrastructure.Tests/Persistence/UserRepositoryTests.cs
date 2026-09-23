using FluentAssertions;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Exceptions;
using JournalApp.Infrastructure.Persistence;
using JournalApp.Infrastructure.Persistence.Repositories;
using JournalApp.Infrastructure.Tests.Fixtures;

namespace JournalApp.Infrastructure.Tests.Persistence;

public class UserRepositoryTests(SqlServerFixture fixture) : DatabaseTestBase(fixture)
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private async Task<User> AddUserAsync(string username, string email)
    {
        await using var context = CreateContext();
        var user = User.Create(username, email, "hash", Now);
        await new UserRepository(context).AddAsync(user, CancellationToken.None);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task AddAsync_NewUser_CanBeReadBackByIdAndEmail()
    {
        // Arrange
        var user = await AddUserAsync("reader", "reader@journal.com");
        await using var context = CreateContext();
        var sut = new UserRepository(context);

        // Act
        var byId = await sut.GetByIdAsync(user.Id, CancellationToken.None);
        var byEmail = await sut.GetByEmailAsync("reader@journal.com", CancellationToken.None);

        // Assert
        byId.Should().NotBeNull();
        byId!.Username.Should().Be("reader");
        byId.CreatedAt.Should().Be(Now);
        byEmail!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task ExistsByEmailAsync_ExistingAndMissingEmails_ReturnsExpected()
    {
        // Arrange
        await AddUserAsync("exists", "exists@journal.com");
        await using var context = CreateContext();
        var sut = new UserRepository(context);

        // Act
        var existing = await sut.ExistsByEmailAsync("exists@journal.com", CancellationToken.None);
        var missing = await sut.ExistsByEmailAsync("missing@journal.com", CancellationToken.None);

        // Assert
        existing.Should().BeTrue();
        missing.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByUsernameAsync_DifferentCasing_ReturnsTrue()
    {
        // Arrange
        await AddUserAsync("CaseUser", "caseuser@journal.com");
        await using var context = CreateContext();
        var sut = new UserRepository(context);

        // Act
        var result = await sut.ExistsByUsernameAsync("caseuser", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        await AddUserAsync("first", "duplicate@journal.com");

        // Act
        var act = () => AddUserAsync("second", "duplicate@journal.com");

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateUsername_ThrowsConflictException()
    {
        // Arrange
        await AddUserAsync("sameuser", "one@journal.com");

        // Act
        var act = () => AddUserAsync("sameuser", "two@journal.com");

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }
}
