using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Abstractions.Persistence;

public enum TaskSortField
{
    CreatedAt,
    DueDate,
}

/// <summary>
/// What to fetch for a task listing. <see cref="UserId"/> always comes from the authenticated user.
/// Due-date bounds form a half-open range: <c>DueAfter &lt;= DueDate &lt; DueBefore</c>, both in UTC.
/// </summary>
public sealed record TaskListCriteria(
    Guid UserId,
    int Page,
    int PageSize,
    TaskStatus? Status = null,
    DateTime? DueAfter = null,
    DateTime? DueBefore = null,
    TaskSortField SortBy = TaskSortField.CreatedAt,
    bool Descending = true)
{
    public int Skip => (Page - 1) * PageSize;
}

public sealed record TaskPage(IReadOnlyList<TaskItem> Items, int TotalCount);
