using JournalApp.Domain.Enums;
using JournalApp.Domain.Exceptions;

namespace JournalApp.Domain.Entities;

public class JournalEntry
{
    public const int TitleMaxLength = 100;
    public const int ContentMaxLength = 5000;

    // Required by EF Core.
    private JournalEntry()
    {
        Title = string.Empty;
        Content = string.Empty;
    }

    private JournalEntry(Guid id, Guid userId, string title, string content, Mood? mood, DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Content = content;
        Mood = mood;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public Mood? Mood { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static JournalEntry Create(Guid userId, string title, string content, Mood? mood, DateTime createdAtUtc)
    {
        var errors = Validate(title, content, mood);
        if (userId == Guid.Empty)
        {
            errors[nameof(UserId)] = ["User id is required."];
        }

        ThrowIfAny(errors);

        return new JournalEntry(Guid.NewGuid(), userId, title.Trim(), content, mood, createdAtUtc);
    }

    public void Update(string title, string content, Mood? mood, DateTime updatedAtUtc)
    {
        ThrowIfAny(Validate(title, content, mood));

        Title = title.Trim();
        Content = content;
        Mood = mood;
        UpdatedAt = updatedAtUtc;
    }

    public bool IsOwnedBy(Guid userId) => UserId == userId;

    private static Dictionary<string, string[]> Validate(string? title, string? content, Mood? mood)
    {
        var errors = new Dictionary<string, string[]>();

        var trimmedTitle = title?.Trim();
        if (string.IsNullOrEmpty(trimmedTitle))
        {
            errors[nameof(Title)] = ["Title is required."];
        }
        else if (trimmedTitle.Length > TitleMaxLength)
        {
            errors[nameof(Title)] = [$"Title must be at most {TitleMaxLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            errors[nameof(Content)] = ["Content is required."];
        }
        else if (content.Length > ContentMaxLength)
        {
            errors[nameof(Content)] = [$"Content must be at most {ContentMaxLength} characters."];
        }

        if (mood is not null && !Enum.IsDefined(mood.Value))
        {
            errors[nameof(Mood)] = ["Mood is not a valid value."];
        }

        return errors;
    }

    private static void ThrowIfAny(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }
    }
}
