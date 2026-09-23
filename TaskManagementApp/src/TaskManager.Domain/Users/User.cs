using TaskManager.Domain.Common;
using TaskManager.Domain.Tasks;

namespace TaskManager.Domain.Users;

public sealed class User
{
    public const int EmailMaxLength = 256;

    private readonly List<TaskItem> _tasks = [];

    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<TaskItem> Tasks => _tasks;

    public static User Create(string email, string passwordHash, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        Guard.AgainstNonUtc(utcNow, nameof(utcNow));

        return new User
        {
            Id = Guid.CreateVersion7(utcNow),
            Email = NormalizeEmail(email),
            PasswordHash = passwordHash,
            CreatedAt = utcNow,
        };
    }

    /// <summary>Canonical form used for storage and lookups (trimmed, lowercased).</summary>
    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > EmailMaxLength)
        {
            throw new DomainException($"Email must be at most {EmailMaxLength} characters.");
        }

        return normalized;
    }
}
