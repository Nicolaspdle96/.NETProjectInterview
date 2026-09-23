using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Abstractions.Persistence;

/// <summary>
/// Task persistence. Every query is scoped to the owning user, so a task belonging
/// to someone else is indistinguishable from a task that does not exist.
/// </summary>
public interface ITaskRepository
{
    /// <summary>Read-only (untracked) lookup of a task owned by <paramref name="userId"/>.</summary>
    Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Tracked lookup for tasks that will be modified or removed.</summary>
    Task<TaskItem?> GetForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Read-only, filtered and sorted page of the user's tasks. When sorting by due date,
    /// tasks without one come last in either direction; ties are broken by id.
    /// </summary>
    Task<TaskPage> ListAsync(TaskListCriteria criteria, CancellationToken cancellationToken);

    void Add(TaskItem task);

    void Remove(TaskItem task);
}
