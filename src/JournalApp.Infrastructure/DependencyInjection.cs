using JournalApp.Application.Common.Interfaces;
using JournalApp.Infrastructure.Caching;
using JournalApp.Infrastructure.Persistence;
using JournalApp.Infrastructure.Persistence.Repositories;
using JournalApp.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JournalApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.Key.Length >= 32, "Jwt:Key must be configured with at least 32 characters.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
                "Jwt:Issuer and Jwt:Audience must be configured.")
            .Validate(o => o.ExpiresMinutes > 0, "Jwt:ExpiresMinutes must be greater than zero.")
            .ValidateOnStart();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<CacheOptions>()
            .Bind(configuration.GetSection(CacheOptions.SectionName))
            .Validate(o => o.ExpirationSeconds > 0, "Cache:ExpirationSeconds must be greater than zero.")
            .Validate(o => o.SizeLimit > 0, "Cache:SizeLimit must be greater than zero.")
            .ValidateOnStart();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }
}
