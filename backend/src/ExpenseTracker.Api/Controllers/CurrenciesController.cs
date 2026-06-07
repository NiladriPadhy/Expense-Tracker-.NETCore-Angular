using ExpenseTracker.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/currencies")]
public sealed class CurrenciesController : ControllerBase
{
    private readonly ICurrencyRepository _repo;
    public CurrenciesController(ICurrencyRepository repo) => _repo = repo;

    public sealed record CurrencyResponse(string Code, string Name, string Symbol, bool IsActive);

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CurrencyResponse>>> GetActive(CancellationToken ct)
    {
        var list = await _repo.ListActiveAsync(ct);
        return Ok(list.Select(c => new CurrencyResponse(c.Code, c.Name, c.Symbol, c.IsActive)).ToList());
    }
}
