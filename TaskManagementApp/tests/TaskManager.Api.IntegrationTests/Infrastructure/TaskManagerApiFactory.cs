using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace TaskManager.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API against a private in-memory SQLite database. The factory holds one
/// connection open for its lifetime so the shared-cache database is not discarded between
/// the connections EF Core opens per request. Migrations run at startup (Development).
/// </summary>
public sealed class TaskManagerApiFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "integration-tests-signing-key-with-plenty-of-bytes";

    private readonly string _connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAliveConnection;

    public TaskManagerApiFactory()
    {
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Jwt:Key", TestJwtKey);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAliveConnection.Dispose();
        }
    }
}
