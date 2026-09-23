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
    [HttpGet]
    [ProducesResponseType<PagedResponse<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int page = ListTasksRequest.DefaultPage,
        [FromQuery(Name = "page_size")] int pageSize = ListTasksRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await taskService.ListAsync(new ListTasksRequest(page, pageSize), cancellationToken);
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
