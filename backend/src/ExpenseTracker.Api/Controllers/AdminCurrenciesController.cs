using ExpenseTracker.Application.Admin.Currencies;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/admin/currencies")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public sealed class AdminCurrenciesController : ControllerBase
{
    private readonly ICurrencyRepository _repo;
    public AdminCurrenciesController(ICurrencyRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminCurrencyDto>>> List(CancellationToken ct)
    {
        var list = await _repo.ListAllAsync(ct);
        return Ok(list.Select(c => new AdminCurrencyDto(c.Code, c.Name, c.Symbol, c.IsActive)).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<AdminCurrencyDto>> Create(
        [FromServices] CreateCurrencyHandler handler,
        [FromBody] CreateCurrencyDto dto, CancellationToken ct)
        => Ok(await handler.HandleAsync(dto, ct));

    [HttpPut("{code}")]
    public async Task<ActionResult<AdminCurrencyDto>> Update(
        [FromServices] UpdateCurrencyHandler handler,
        string code, [FromBody] UpdateCurrencyDto dto, CancellationToken ct)
        => Ok(await handler.HandleAsync(code, dto, ct));

    [HttpDelete("{code}")]
    public async Task<IActionResult> Deactivate(
        [FromServices] DeactivateCurrencyHandler handler,
        string code, [FromQuery] bool hard = false, CancellationToken ct = default)
    {
        await handler.HandleAsync(code, hard, ct);
        return NoContent();
    }
}
