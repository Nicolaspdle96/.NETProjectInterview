using FluentAssertions;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Enums;
using JournalApp.Infrastructure.Persistence;
using JournalApp.Infrastructure.Persistence.Repositories;
using JournalApp.Infrastructure.Tests.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Infrastructure.Tests.Persistence;

public class JournalEntryRepositoryTests(SqlServerFixture fixture) : DatabaseTestBase(fixture)
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private async Task<User> AddUserAsync(string username)
    {
        await using var context = CreateContext();
        var user = User.Create(username, $"{username}@journal.com", "hash", Now);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task<JournalEntry> AddEntryAsync(Guid userId, string title, DateTime createdAt, Mood? mood = null)
    {
        await using var context = CreateContext();
        var entry = JournalEntry.Create(userId, title, "Content", mood, createdAt);
        await new JournalEntryRepository(context).AddAsync(entry, CancellationToken.None);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
        return entry;
    }

    [Fact]
    public async Task ListByUserAsync_EntriesOfSeveralUsers_ReturnsOnlyOwnEntriesNewestFirst()
    {
        // Arrange
        var owner = await AddUserAsync("owner");
        var other = await AddUserAsync("other");
        await AddEntryAsync(owner.Id, "Middle", Now.AddDays(-2));
        await AddEntryAsync(owner.Id, "Newest", Now);
        await AddEntryAsync(owner.Id, "Oldest", Now.AddDays(-5));
        await AddEntryAsync(other.Id, "Not mine", Now.AddDays(1));
        await using var context = CreateContext();

        // Act
        var result = await new JournalEntryRepository(context).ListByUserAsync(owner.Id, CancellationToken.None);

        // Assert
        result.Select(e => e.Title).Should().Equal("Newest", "Middle", "Oldest");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEntry_ReturnsEntryWithMood()
    {
        // Arrange
        var user = await AddUserAsync("getter");
        var entry = await AddEntryAsync(user.Id, "Title", Now, Mood.Great);
        await using var context = CreateContext();

        // Act
        var result = await new JournalEntryRepository(context).GetByIdAsync(entry.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Mood.Should().Be(Mood.Great);
        result.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Mood_IsStoredAsReadableString()
    {
        // Arrange
        var user = await AddUserAsync("moody");
        var entry = await AddEntryAsync(user.Id, "Title", Now, Mood.Awful);
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT Mood FROM JournalEntries WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", entry.Id);

        // Act
        var stored = await command.ExecuteScalarAsync();

        // Assert
        stored.Should().Be("Awful");
    }

    [Fact]
    public async Task Remove_ExistingEntry_DeletesIt()
    {
        // Arrange
        var user = await AddUserAsync("remover");
        var entry = await AddEntryAsync(user.Id, "To delete", Now);
        await using (var context = CreateContext())
        {
            var repository = new JournalEntryRepository(context);
            var loaded = await repository.GetByIdAsync(entry.Id, CancellationToken.None);

            // Act
            repository.Remove(loaded!);
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
        }

        // Assert
        await using var verify = CreateContext();
        (await verify.JournalEntries.AnyAsync(e => e.Id == entry.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeletingUser_CascadesToEntries()
    {
        // Arrange
        var user = await AddUserAsync("cascade");
        await AddEntryAsync(user.Id, "Entry", Now);
        await using var context = CreateContext();

        // Act
        await context.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();

        // Assert
        (await context.JournalEntries.AnyAsync(e => e.UserId == user.Id)).Should().BeFalse();
    }
}
