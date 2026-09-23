using JournalApp.Application.Common.Interfaces;
using JournalApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace JournalApp.Infrastructure.Security;

/// <summary>
/// Adapter over ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2 with a random salt).
/// Only the hasher is used, not the rest of Identity.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string passwordHash, string providedPassword) =>
        _inner.VerifyHashedPassword(null!, passwordHash, providedPassword) != PasswordVerificationResult.Failed;
}
