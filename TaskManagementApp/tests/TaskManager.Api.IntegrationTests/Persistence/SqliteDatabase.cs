using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Api.IntegrationTests.Persistence;

/// <summary>
/// Base class for persistence tests: each test gets a fresh in-memory SQLite database
/// with the real migrations applied. The connection stays open so the database survives
/// across the multiple contexts a test creates.
/// </summary>
public abstract class SqliteDatabase : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected static readonly DateTime Now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    protected AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
}
