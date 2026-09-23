using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Auth;

namespace TaskManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
