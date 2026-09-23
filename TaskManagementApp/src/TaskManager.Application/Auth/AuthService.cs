using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Common.Results;
using TaskManager.Application.Common.Validation;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Auth;

internal sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : IAuthService
{
    private const string BearerTokenType = "Bearer";

    public async Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var email = User.NormalizeEmail(request.Email);
        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            return AuthErrors.EmailAlreadyRegistered;
        }

        var user = User.Create(email, passwordHasher.Hash(request.Password), dateTimeProvider.UtcNow);
        users.Add(user);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            return AuthErrors.EmailAlreadyRegistered;
        }

        return new RegisterResponse(user.Id, user.Email);
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return AuthErrors.InvalidCredentials;
        }

        var token = tokenGenerator.Generate(user);
        return new LoginResponse(token.Token, BearerTokenType, token.ExpiresAt);
    }

    public async Task<Result<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        // A valid token for a user that no longer exists is treated as unauthenticated.
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return AuthErrors.NotAuthenticated;
        }

        return new CurrentUserResponse(user.Id, user.Email, user.CreatedAt);
    }
}
