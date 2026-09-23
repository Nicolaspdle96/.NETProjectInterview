using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TaskManager.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC-SHA256 signing key. Never committed: user-secrets in development, <c>Jwt__Key</c> elsewhere.</summary>
    public string Key { get; init; } = string.Empty;

    public int ExpiryMinutes { get; init; } = 60;

    public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(Key));

    internal bool HasValidKey() => Encoding.UTF8.GetByteCount(Key) >= MinimumKeyBytes;
}
