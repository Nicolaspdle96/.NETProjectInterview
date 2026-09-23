using FluentValidation;
using JournalApp.Application.Auth.Dtos;
using JournalApp.Application.Common;
using JournalApp.Application.Common.Exceptions;
using JournalApp.Application.Common.Interfaces;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Exceptions;

namespace JournalApp.Application.Auth;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken);
}

public class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    TimeProvider timeProvider) : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        await registerValidator.EnsureValidAsync(request, cancellationToken);

        var email = User.NormalizeEmail(request.Email);
        var username = request.Username.Trim();

        if (await users.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new ConflictException("The email is already in use.");
        }

        if (await users.ExistsByUsernameAsync(username, cancellationToken))
        {
            throw new ConflictException("The username is already in use.");
        }

        var user = User.Create(
            username,
            email,
            passwordHasher.Hash(request.Password),
            timeProvider.GetUtcNow().UtcDateTime);

        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await loginValidator.EnsureValidAsync(request, cancellationToken);

        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);
        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var token = tokenService.GenerateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAt, user.ToDto());
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("The authenticated user no longer exists.");

        return user.ToDto();
    }
}
