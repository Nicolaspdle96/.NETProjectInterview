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

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(task => task.CreatedAt)
            .ThenByDescending(task => task.Id)
            .Skip(criteria.Skip)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return new TaskPage(items, totalCount);
    }

    public void Add(TaskItem task) => dbContext.Tasks.Add(task);

    public void Remove(TaskItem task) => dbContext.Tasks.Remove(task);
}
