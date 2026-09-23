using TaskManager.Application.Common;
using TaskManager.Application.Common.Results;

namespace TaskManager.Application.Tasks;

/// <summary>Task use cases, always scoped to the authenticated user.</summary>
public interface ITaskService
{
    Task<Result<PagedResponse<TaskResponse>>> ListAsync(ListTasksRequest request, CancellationToken cancellationToken);

    Task<Result<TaskResponse>> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<TaskResponse>> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken);

    Task<Result<TaskResponse>> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
