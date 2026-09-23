using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Auth;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Results;
using TaskManager.Application.Common.Validation;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

internal sealed class TaskService(
    ITaskRepository tasks,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<ListTasksRequest> listValidator) : ITaskService
{
    public async Task<Result<PagedResponse<TaskResponse>>> ListAsync(ListTasksRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        var validation = await listValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        TaskSort.TryParse(request.Sort, out var sort);
        var criteria = new TaskListCriteria(
            userId,
            request.Page,
            request.PageSize,
            request.Status,
            request.DueAfter.AsUtc(),
            request.DueBefore.AsUtc(),
            sort.Field,
            sort.Descending);

        var page = await tasks.ListAsync(criteria, cancellationToken);

        return new PagedResponse<TaskResponse>(
            page.Items.Select(task => task.ToResponse()).ToList(),
            request.Page,
            request.PageSize,
            page.TotalCount);
    }

    public async Task<Result<TaskResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        var task = await tasks.GetByIdAsync(id, userId, cancellationToken);
        return task is null ? TaskErrors.NotFound : task.ToResponse();
    }

    public async Task<Result<TaskResponse>> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var task = TaskItem.Create(
            userId,
            request.Title,
            request.Description,
            request.Status ?? TaskStatus.Todo,
            request.DueDate.AsUtc(),
            dateTimeProvider.UtcNow);

        tasks.Add(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }

    public async Task<Result<TaskResponse>> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var task = await tasks.GetForUpdateAsync(id, userId, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound;
        }

        task.Update(request.Title, request.Description, request.Status!.Value, request.DueDate.AsUtc(), dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        var task = await tasks.GetForUpdateAsync(id, userId, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound;
        }

        tasks.Remove(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
