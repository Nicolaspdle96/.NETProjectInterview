using JournalApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace JournalApp.Api.Tests.Fixtures;

/// <summary>
/// Hosts the real API against an ephemeral SQL Server started with Testcontainers.
/// Set JOURNALAPP_TEST_SQLSERVER to an existing server's connection string (e.g. LocalDB)
/// to run the suite without Docker.
/// </summary>
public sealed class JournalApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ExternalServerVariable = "JOURNALAPP_TEST_SQLSERVER";

    private MsSqlContainer? _container;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        var serverConnectionString = Environment.GetEnvironmentVariable(ExternalServerVariable);
        if (string.IsNullOrWhiteSpace(serverConnectionString))
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
            await _container.StartAsync();
            serverConnectionString = _container.GetConnectionString();
        }

        _connectionString = new SqlConnectionStringBuilder(serverConnectionString)
        {
            InitialCatalog = $"JournalAppApiTests_{Guid.NewGuid():N}",
            TrustServerCertificate = true
        }.ConnectionString;

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Jwt:Issuer", "JournalApp.Tests");
        builder.UseSetting("Jwt:Audience", "JournalApp.Tests.Client");
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-that-is-long-enough-0123456789");
        builder.UseSetting("Jwt:ExpiresMinutes", "30");
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<JournalApiFactory>
{
    public const string Name = "Api";
}
