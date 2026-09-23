using JournalApp.Application.Auth.Dtos;
using JournalApp.Application.Common;
using JournalApp.Application.Common.Caching;
using JournalApp.Application.Common.Interfaces;

namespace JournalApp.Application.Auth;

/// <summary>
/// Caching decorator over <see cref="AuthService"/>. Only the current user's profile is cached
/// (users can't be edited); register and login always go to the database.
/// </summary>
public class CachedAuthService(
    IAuthService inner,
    ICacheService cache,
    ICurrentUserService currentUser) : IAuthService
{
    public Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        inner.RegisterAsync(request, cancellationToken);

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        inner.LoginAsync(request, cancellationToken);

    public Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        return cache.GetOrCreateAsync(
            CacheKeys.CurrentUser(userId),
            inner.GetCurrentUserAsync,
            cancellationToken);
    }
}
