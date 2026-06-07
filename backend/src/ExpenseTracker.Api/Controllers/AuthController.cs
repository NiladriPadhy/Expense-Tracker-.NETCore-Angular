using ExpenseTracker.Api.Authorization;
using ExpenseTracker.Application.Auth;
using ExpenseTracker.Application.Auth.Dtos;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("multipart/form-data", "application/json")]
    public async Task<ActionResult<AuthResult>> Register(
        [FromServices] RegisterUserHandler handler,
        [FromForm] string fullName,
        [FromForm] string email,
        [FromForm] string phone,
        [FromForm] string password,
        [FromForm] string currencyCode,
        IFormFile? photo,
        CancellationToken ct)
    {
        var req = new RegisterUserRequest(fullName, email, phone, password, currencyCode);
        Stream? stream = null;
        string? mime = null;
        if (photo is not null)
        {
            stream = photo.OpenReadStream();
            mime = photo.ContentType;
        }
        var result = await handler.HandleAsync(req, stream, mime, ct);
        return Ok(result);
    }

    [HttpPost("register-json")]
    [AllowAnonymous]
    public Task<ActionResult<AuthResult>> RegisterJson(
        [FromServices] RegisterUserHandler handler,
        [FromBody] RegisterUserRequest req,
        CancellationToken ct)
        => handler.HandleAsync(req, null, null, ct).ContinueWith<ActionResult<AuthResult>>(t => Ok(t.Result), ct);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Login(
        [FromServices] LoginUserHandler handler,
        [FromBody] LoginRequest req,
        CancellationToken ct)
        => Ok(await handler.HandleAsync(req, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Refresh(
        [FromServices] RefreshTokenHandler handler,
        [FromBody] RefreshRequest req,
        CancellationToken ct)
        => Ok(await handler.HandleAsync(req, ct));

    [HttpPost("logout")]
    [Authorize(Policy = PolicyNames.RequireUser)]
    public async Task<IActionResult> Logout(
        [FromServices] LogoutHandler handler,
        [FromBody] RefreshRequest req,
        CancellationToken ct)
    {
        await handler.HandleAsync(req.RefreshToken, ct);
        return NoContent();
    }
}
