using JournalApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Infrastructure.Tests.Fixtures;

/// <summary>
/// Points the tests at a real SQL Server (LocalDB by default). Each test class gets its own
/// database so tests don't interfere with each other.
/// Set JOURNALAPP_TEST_SQLSERVER to another server's connection string to override it.
/// </summary>
public sealed class SqlServerFixture
{
    public const string ServerVariable = "JOURNALAPP_TEST_SQLSERVER";
    public const string DefaultServer = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True";

    private readonly string _serverConnectionString =
        Environment.GetEnvironmentVariable(ServerVariable) is { Length: > 0 } configured ? configured : DefaultServer;

    public string CreateDatabaseConnectionString(string prefix)
    {
        var builder = new SqlConnectionStringBuilder(_serverConnectionString)
        {
            InitialCatalog = $"{prefix}_{Guid.NewGuid():N}",
            TrustServerCertificate = true
        };
        return builder.ConnectionString;
    }

    public static AppDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options);
}

[CollectionDefinition(Name)]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}

/// <summary>
/// Base class that creates a fresh, migrated database per test class and drops it afterwards.
/// </summary>
[Collection(SqlServerCollection.Name)]
public abstract class DatabaseTestBase(SqlServerFixture fixture) : IAsyncLifetime
{
    protected string ConnectionString { get; } = fixture.CreateDatabaseConnectionString("JournalAppInfraTests");

    protected AppDbContext CreateContext() => SqlServerFixture.CreateContext(ConnectionString);

    public virtual async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
