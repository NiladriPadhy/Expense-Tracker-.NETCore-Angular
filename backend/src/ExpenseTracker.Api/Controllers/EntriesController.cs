using ExpenseTracker.Api.Authorization;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Entries;
using ExpenseTracker.Application.Entries.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/entries")]
[Authorize(Policy = PolicyNames.RequireUser)]
[EnableRateLimiting("user")]
public sealed class EntriesController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<EntryDto>> Create(
        [FromServices] CreateEntryHandler handler,
        [FromBody] EntryCreateDto dto,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(User.GetUserId(), dto, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EntryDto>> Get(
        [FromServices] GetEntryHandler handler,
        Guid id,
        CancellationToken ct)
        => Ok(await handler.HandleAsync(User.GetUserId(), id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EntryDto>> Update(
        [FromServices] UpdateEntryHandler handler,
        Guid id,
        [FromBody] EntryUpdateDto dto,
        CancellationToken ct)
        => Ok(await handler.HandleAsync(User.GetUserId(), id, dto, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromServices] DeleteEntryHandler handler,
        Guid id,
        CancellationToken ct)
    {
        await handler.HandleAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
