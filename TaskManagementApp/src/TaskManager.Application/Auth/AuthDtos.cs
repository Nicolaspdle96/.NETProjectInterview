namespace TaskManager.Application.Auth;

public sealed record RegisterRequest(string Email, string Password);

public sealed record RegisterResponse(Guid Id, string Email);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAt);

public sealed record CurrentUserResponse(Guid Id, string Email, DateTime CreatedAt);
