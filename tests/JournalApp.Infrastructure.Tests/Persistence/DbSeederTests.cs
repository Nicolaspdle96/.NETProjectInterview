using FluentAssertions;
using JournalApp.Infrastructure.Persistence;
using JournalApp.Infrastructure.Security;
using JournalApp.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Infrastructure.Tests.Persistence;

public class DbSeederTests(SqlServerFixture fixture) : DatabaseTestBase(fixture)
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesDemoUsersAndEntries()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        await DbSeeder.SeedAsync(context, _hasher, TimeProvider.System, CancellationToken.None);

        // Assert
        var demo = await context.Users.SingleAsync(u => u.Email == DbSeeder.DemoEmail);
        var alex = await context.Users.SingleAsync(u => u.Email == DbSeeder.AlexEmail);
        demo.Username.Should().Be("demo");
        _hasher.Verify(demo.PasswordHash, DbSeeder.DemoPassword).Should().BeTrue();
        _hasher.Verify(alex.PasswordHash, DbSeeder.AlexPassword).Should().BeTrue();

        var demoEntries = await context.JournalEntries.Where(e => e.UserId == demo.Id).ToListAsync();
        demoEntries.Should().HaveCount(5);
        demoEntries.Select(e => e.Mood).Distinct().Should().HaveCountGreaterThan(1);
        demoEntries.Select(e => e.CreatedAt).Distinct().Should().HaveCount(5);
        (await context.JournalEntries.CountAsync(e => e.UserId == alex.Id)).Should().Be(2);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateData()
    {
        // Arrange
        await using (var first = CreateContext())
        {
            await DbSeeder.SeedAsync(first, _hasher, TimeProvider.System, CancellationToken.None);
        }

        await using var context = CreateContext();

        // Act
        await DbSeeder.SeedAsync(context, _hasher, TimeProvider.System, CancellationToken.None);

        // Assert
        (await context.Users.CountAsync()).Should().Be(2);
        (await context.JournalEntries.CountAsync()).Should().Be(7);
    }
}
