using JournalApp.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace JournalApp.Infrastructure.Tests.Fixtures;

/// <summary>
/// Starts one ephemeral SQL Server container for the whole test collection. Each test class
/// gets its own database so tests don't interfere with each other.
/// Set JOURNALAPP_TEST_SQLSERVER to an existing server's connection string (e.g. LocalDB)
/// to run the suite without Docker.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string ExternalServerVariable = "JOURNALAPP_TEST_SQLSERVER";

    private MsSqlContainer? _container;
    private string _serverConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable(ExternalServerVariable);
        if (!string.IsNullOrWhiteSpace(external))
        {
            _serverConnectionString = external;
            return;
        }

        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _container.StartAsync();
        _serverConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

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
