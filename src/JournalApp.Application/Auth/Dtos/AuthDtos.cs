namespace JournalApp.Application.Auth.Dtos;

public record RegisterRequest(string Username, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record UserDto(Guid Id, string Username, string Email, DateTime CreatedAt);

public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
