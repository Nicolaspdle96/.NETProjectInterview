using JournalApp.Application.Entries;
using JournalApp.Application.Entries.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JournalApp.Api.Controllers;

[ApiController]
[Route("api/entries")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class EntriesController(IJournalEntryService entryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EntryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EntryDto>>> List(CancellationToken cancellationToken) =>
        Ok(await entryService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EntryDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await entryService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<EntryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EntryDto>> Create(CreateEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await entryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = entry.Id }, entry);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<EntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EntryDto>> Update(Guid id, UpdateEntryRequest request, CancellationToken cancellationToken) =>
        Ok(await entryService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await entryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
