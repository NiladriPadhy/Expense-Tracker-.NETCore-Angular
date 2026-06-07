using ExpenseTracker.Application.Admin.Users;
using ExpenseTracker.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
[EnableRateLimiting("user")]
public sealed class AdminUsersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> List(
        [FromServices] ListUsersHandler handler,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => Ok(await handler.HandleAsync(search, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> Get(
        [FromServices] GetUserHandler handler,
        Guid id, CancellationToken ct)
        => Ok(await handler.HandleAsync(id, ct));

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> Update(
        [FromServices] UpdateUserHandler handler,
        Guid id,
        [FromBody] AdminUserUpdateDto dto,
        CancellationToken ct)
        => Ok(await handler.HandleAsync(id, dto, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromServices] DeleteUserHandler handler,
        Guid id,
        [FromQuery] bool hard = false,
        CancellationToken ct = default)
    {
        await handler.HandleAsync(id, hard, ct);
        return NoContent();
    }
}
