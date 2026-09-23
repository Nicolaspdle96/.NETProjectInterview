using FluentValidation;
using JournalApp.Application.Auth;
using JournalApp.Application.Entries;
using Microsoft.Extensions.DependencyInjection;

namespace JournalApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: false);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();

        return services;
    }
}
