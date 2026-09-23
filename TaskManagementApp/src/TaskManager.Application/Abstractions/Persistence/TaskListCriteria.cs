using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Abstractions.Persistence;

/// <summary>What to fetch for a task listing. <see cref="UserId"/> always comes from the authenticated user.</summary>
public sealed record TaskListCriteria(Guid UserId, int Page, int PageSize)
{
    public int Skip => (Page - 1) * PageSize;
}

public sealed record TaskPage(IReadOnlyList<TaskItem> Items, int TotalCount);
