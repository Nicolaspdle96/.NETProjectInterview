using JournalApp.Domain.Entities;

namespace JournalApp.Application.Common.Interfaces;

public record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken GenerateToken(User user);
}
