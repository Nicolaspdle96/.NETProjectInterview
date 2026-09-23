using TaskManager.Application.Common.Results;

namespace TaskManager.Application.Auth;

public interface IAuthService
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Result<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken);
}
