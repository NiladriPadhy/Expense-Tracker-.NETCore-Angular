using ExpenseTracker.Api.Authorization;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Dashboard;
using ExpenseTracker.Application.Dashboard.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Policy = PolicyNames.RequireUser)]
[EnableRateLimiting("user")]
public sealed class DashboardController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(
        [FromServices] GetDashboardHandler handler,
        [FromQuery] int monthsBack = 6,
        CancellationToken ct = default)
        => Ok(await handler.HandleAsync(User.GetUserId(), monthsBack, ct));
}
