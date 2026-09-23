namespace JournalApp.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 signing key (at least 32 characters). Keep it in user secrets or environment
    /// variables outside Development.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 60;
}
