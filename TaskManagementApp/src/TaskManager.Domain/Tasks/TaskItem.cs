using TaskManager.Domain.Common;

namespace TaskManager.Domain.Tasks;

public sealed class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private TaskItem()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public TaskStatus Status { get; private set; }

    public DateTime? DueDate { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static TaskItem Create(
        Guid userId,
        string title,
        string? description,
        TaskStatus status,
        DateTime? dueDate,
        DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("A task must belong to a user.");
        }

        Guard.AgainstNonUtc(utcNow, nameof(utcNow));

        var task = new TaskItem
        {
            Id = Guid.CreateVersion7(utcNow),
            UserId = userId,
            CreatedAt = utcNow,
        };
        task.Apply(title, description, status, dueDate);
        return task;
    }

    public void Update(string title, string? description, TaskStatus status, DateTime? dueDate, DateTime utcNow)
    {
        Guard.AgainstNonUtc(utcNow, nameof(utcNow));
        Apply(title, description, status, dueDate);
        UpdatedAt = utcNow;
    }

    public void ChangeStatus(TaskStatus status, DateTime utcNow)
    {
        Guard.AgainstNonUtc(utcNow, nameof(utcNow));
        Status = ValidateStatus(status);
        UpdatedAt = utcNow;
    }

    private void Apply(string title, string? description, TaskStatus status, DateTime? dueDate)
    {
        Title = ValidateTitle(title);
        Description = ValidateDescription(description);
        Status = ValidateStatus(status);
        DueDate = ValidateDueDate(dueDate);
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }

        var trimmed = title.Trim();
        if (trimmed.Length > TitleMaxLength)
        {
            throw new DomainException($"Title must be at most {TitleMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > DescriptionMaxLength)
        {
            throw new DomainException($"Description must be at most {DescriptionMaxLength} characters.");
        }

        return trimmed;
    }

    private static TaskStatus ValidateStatus(TaskStatus status) =>
        Enum.IsDefined(status) ? status : throw new DomainException($"'{status}' is not a valid task status.");

    private static DateTime? ValidateDueDate(DateTime? dueDate)
    {
        if (dueDate is { } value)
        {
            Guard.AgainstNonUtc(value, nameof(DueDate));
        }

        return dueDate;
    }
}
