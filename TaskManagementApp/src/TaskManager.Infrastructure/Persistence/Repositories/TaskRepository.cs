using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Tasks;

namespace TaskManager.Infrastructure.Persistence.Repositories;

internal sealed class TaskRepository(AppDbContext dbContext) : ITaskRepository
{
    public Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Tasks
            .AsNoTracking()
            .SingleOrDefaultAsync(task => task.Id == id && task.UserId == userId, cancellationToken);

    public Task<TaskItem?> GetForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Tasks
            .SingleOrDefaultAsync(task => task.Id == id && task.UserId == userId, cancellationToken);

    public async Task<TaskPage> ListAsync(TaskListCriteria criteria, CancellationToken cancellationToken)
    {
        var query = dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.UserId == criteria.UserId);

        if (criteria.Status is { } status)
        {
            query = query.Where(task => task.Status == status);
        }

        if (criteria.DueAfter is { } dueAfter)
        {
            query = query.Where(task => task.DueDate >= dueAfter);
        }

        if (criteria.DueBefore is { } dueBefore)
        {
            query = query.Where(task => task.DueDate < dueBefore);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await Sort(query, criteria)
            .Skip(criteria.Skip)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return new TaskPage(items, totalCount);
    }

    private static IQueryable<TaskItem> Sort(IQueryable<TaskItem> query, TaskListCriteria criteria)
    {
        var ordered = (criteria.SortBy, criteria.Descending) switch
        {
            // Tasks without a due date go last in both directions.
            (TaskSortField.DueDate, false) => query.OrderBy(task => task.DueDate == null).ThenBy(task => task.DueDate),
            (TaskSortField.DueDate, true) => query.OrderBy(task => task.DueDate == null).ThenByDescending(task => task.DueDate),
            (_, false) => query.OrderBy(task => task.CreatedAt),
            _ => query.OrderByDescending(task => task.CreatedAt),
        };

        // Deterministic tie-breaker so pages never overlap or skip rows.
        return criteria.Descending ? ordered.ThenByDescending(task => task.Id) : ordered.ThenBy(task => task.Id);
    }

    public void Add(TaskItem task) => dbContext.Tasks.Add(task);

    public void Remove(TaskItem task) => dbContext.Tasks.Remove(task);
}
