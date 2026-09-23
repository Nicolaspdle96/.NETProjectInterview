using System.Text.RegularExpressions;
using JournalApp.Domain.Exceptions;

namespace JournalApp.Domain.Entities;

public partial class User
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 30;
    public const int EmailMaxLength = 256;

    // Required by EF Core.
    private User()
    {
        Username = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    private User(Guid id, string username, string email, string passwordHash, DateTime createdAt)
    {
        Id = id;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<JournalEntry> Entries { get; private set; } = new List<JournalEntry>();

    public static User Create(string username, string email, string passwordHash, DateTime createdAtUtc)
    {
        var errors = new Dictionary<string, string[]>();

        var normalizedUsername = username?.Trim() ?? string.Empty;
        if (normalizedUsername.Length is < UsernameMinLength or > UsernameMaxLength)
        {
            errors[nameof(Username)] =
                [$"Username must be between {UsernameMinLength} and {UsernameMaxLength} characters."];
        }

        var normalizedEmail = NormalizeEmail(email);
        if (!IsValidEmail(normalizedEmail))
        {
            errors[nameof(Email)] = ["Email is not a valid email address."];
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            errors[nameof(PasswordHash)] = ["Password hash is required."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        return new User(Guid.NewGuid(), normalizedUsername, normalizedEmail, passwordHash, createdAtUtc);
    }

    /// <summary>
    /// Emails are compared case-insensitively, so they are always stored trimmed and lowercased.
    /// </summary>
    public static string NormalizeEmail(string? email) => email?.Trim().ToLowerInvariant() ?? string.Empty;

    public static bool IsValidEmail(string email) =>
        email.Length <= EmailMaxLength && EmailRegex().IsMatch(email);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
