using JournalApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JournalApp.Api.Tests.Fixtures;

/// <summary>
/// Hosts the real API against a throwaway database on a real SQL Server (LocalDB by default).
/// Set JOURNALAPP_TEST_SQLSERVER to another server's connection string to override it.
/// </summary>
public sealed class JournalApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ServerVariable = "JOURNALAPP_TEST_SQLSERVER";
    public const string DefaultServer = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True";

    private readonly string _connectionString = new SqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable(ServerVariable) is { Length: > 0 } configured ? configured : DefaultServer)
    {
        InitialCatalog = $"JournalAppApiTests_{Guid.NewGuid():N}",
        TrustServerCertificate = true
    }.ConnectionString;

    public async Task InitializeAsync()
    {
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
