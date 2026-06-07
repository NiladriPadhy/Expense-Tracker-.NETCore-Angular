using ExpenseTracker.Api.Authorization;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Months;
using ExpenseTracker.Application.Months.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/months")]
[Authorize(Policy = PolicyNames.RequireUser)]
[EnableRateLimiting("user")]
public sealed class MonthsController : ControllerBase
{
    [HttpGet("{year:int}/{month:int}")]
    public async Task<ActionResult<MonthlyViewDto>> Get(
        [FromServices] GetMonthlyViewHandler handler,
        int year,
        int month,
        CancellationToken ct)
    {
        if (month < 1 || month > 12) return BadRequest();
        return Ok(await handler.HandleAsync(User.GetUserId(), year, month, ct));
    }
}
