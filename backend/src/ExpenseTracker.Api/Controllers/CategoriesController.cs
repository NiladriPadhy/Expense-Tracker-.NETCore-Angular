using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize(Policy = PolicyNames.RequireUser)]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repo;
    public CategoriesController(ICategoryRepository repo) => _repo = repo;

    public sealed record CategoryResponse(Guid Id, string Name, EntryType Type, bool IsActive);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> List([FromQuery] EntryType? type, CancellationToken ct)
    {
        var list = await _repo.ListActiveAsync(type, ct);
        return Ok(list.Select(c => new CategoryResponse(c.Id, c.Name, c.Type, c.IsActive)).ToList());
    }
}
