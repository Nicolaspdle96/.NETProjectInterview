using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common;
using TaskManager.Application.Tasks;

namespace TaskManager.Api.Controllers;

[Route("api/tasks")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class TasksController(ITaskService taskService) : ApiControllerBase
{
    // [Description] rather than XML <param> docs: those are matched by C# name and get lost on renamed query keys.
    [HttpGet]
    [ProducesResponseType<PagedResponse<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery, Description("Only tasks with this status.")] TaskStatus? status,
        [FromQuery(Name = "due_after"), Description("Only tasks due at or after this instant (inclusive).")] DateTime? dueAfter,
        [FromQuery(Name = "due_before"), Description("Only tasks due before this instant (exclusive).")] DateTime? dueBefore,
        [FromQuery, Description("created_at, -created_at (default), due_date or -due_date. Tasks without a due date sort last.")] string? sort,
        [FromQuery, Description("1-based page number.")] int page = ListTasksRequest.DefaultPage,
        [FromQuery(Name = "page_size"), Description("Items per page, 1 to 100.")] int pageSize = ListTasksRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var request = new ListTasksRequest(page, pageSize, status, dueAfter, dueBefore, sort);
        var result = await taskService.ListAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await taskService.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await taskService.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await taskService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await taskService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }
}
