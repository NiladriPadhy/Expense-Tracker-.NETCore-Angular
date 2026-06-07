using ExpenseTracker.Application.Admin.Categories;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/admin/categories")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public sealed class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repo;
    public AdminCategoriesController(ICategoryRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryDto>>> List(CancellationToken ct)
    {
        var list = await _repo.ListAllAsync(ct);
        return Ok(list.Select(c => new AdminCategoryDto(c.Id, c.Name, c.Type, c.IsActive)).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<AdminCategoryDto>> Create(
        [FromServices] CreateCategoryHandler handler,
        [FromBody] CreateCategoryDto dto, CancellationToken ct)
        => Ok(await handler.HandleAsync(dto, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminCategoryDto>> Update(
        [FromServices] UpdateCategoryHandler handler,
        Guid id, [FromBody] UpdateCategoryDto dto, CancellationToken ct)
        => Ok(await handler.HandleAsync(id, dto, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(
        [FromServices] DeactivateCategoryHandler handler,
        Guid id, CancellationToken ct)
    {
        await handler.HandleAsync(id, ct);
        return NoContent();
    }
}
