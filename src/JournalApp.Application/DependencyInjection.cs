using FluentValidation;
using JournalApp.Application.Auth;
using JournalApp.Application.Common.Interfaces;
using JournalApp.Application.Entries;
using Microsoft.Extensions.DependencyInjection;

namespace JournalApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: false);

        // Controllers get the caching decorators, which delegate to the real services on a miss.
        services.AddScoped<AuthService>();
        services.AddScoped<IAuthService>(sp => new CachedAuthService(
            sp.GetRequiredService<AuthService>(),
            sp.GetRequiredService<ICacheService>(),
            sp.GetRequiredService<ICurrentUserService>()));

        services.AddScoped<JournalEntryService>();
        services.AddScoped<IJournalEntryService>(sp => new CachedJournalEntryService(
            sp.GetRequiredService<JournalEntryService>(),
            sp.GetRequiredService<ICacheService>(),
            sp.GetRequiredService<ICurrentUserService>()));

        return services;
    }
}
