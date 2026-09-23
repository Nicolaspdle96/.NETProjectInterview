using TaskManager.Domain.Users;

namespace TaskManager.Application.Abstractions.Authentication;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}
