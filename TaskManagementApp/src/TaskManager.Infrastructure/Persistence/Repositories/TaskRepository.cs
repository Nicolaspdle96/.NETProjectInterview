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

    public async Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.UserId == userId)
            .OrderByDescending(task => task.CreatedAt)
            .ThenByDescending(task => task.Id)
            .ToListAsync(cancellationToken);

    public void Add(TaskItem task) => dbContext.Tasks.Add(task);

    public void Remove(TaskItem task) => dbContext.Tasks.Remove(task);
}
