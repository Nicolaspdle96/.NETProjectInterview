using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

/// <param name="Status">Optional; defaults to <see cref="TaskStatus.Todo"/>.</param>
public sealed record CreateTaskRequest(string Title, string? Description, TaskStatus? Status, DateTime? DueDate);

/// <param name="Status">Required: nullable only so that a missing value is reported instead of silently becoming <c>Todo</c>.</param>
public sealed record UpdateTaskRequest(string Title, string? Description, TaskStatus? Status, DateTime? DueDate);

public sealed record ListTasksRequest(int Page = ListTasksRequest.DefaultPage, int PageSize = ListTasksRequest.DefaultPageSize)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status, task.DueDate, task.CreatedAt, task.UpdatedAt);
}
