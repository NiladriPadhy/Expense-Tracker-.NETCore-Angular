using ExpenseTracker.Api.Authorization;
using ExpenseTracker.Application.Auth;
using ExpenseTracker.Application.Auth.Dtos;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize(Policy = PolicyNames.RequireUser)]
[EnableRateLimiting("user")]
public sealed class MeController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ICurrencyRepository _currencies;
    private readonly IUnitOfWork _uow;
    private readonly IPhotoStorage _photoStorage;
    private readonly IClock _clock;

    public MeController(IUserRepository users, ICurrencyRepository currencies, IUnitOfWork uow, IPhotoStorage photoStorage, IClock clock)
    {
        _users = users; _currencies = currencies; _uow = uow; _photoStorage = photoStorage; _clock = clock;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> Get(CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(User.GetUserId(), ct)
            ?? throw new NotFoundException("user_not_found", "User not found.");
        return Ok(RegisterUserHandler.ToProfile(u));
    }

    public sealed record UpdateMeDto(string? FullName, string? Phone, string? CurrencyCode);

    [HttpPatch]
    public async Task<ActionResult<UserProfileDto>> Update([FromBody] UpdateMeDto dto, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(User.GetUserId(), ct)
            ?? throw new NotFoundException("user_not_found", "User not found.");
        if (!string.IsNullOrWhiteSpace(dto.CurrencyCode))
        {
            var cur = await _currencies.GetByCodeAsync(dto.CurrencyCode, ct);
            if (cur is null || !cur.IsActive)
            {
                throw new AppException("invalid_currency", "Currency unavailable.");
            }
        }
        u.UpdateProfile(dto.FullName ?? u.FullName, dto.Phone ?? u.Phone, dto.CurrencyCode ?? u.CurrencyCode, _clock.UtcNow);
        _users.Update(u);
        await _uow.SaveChangesAsync(ct);
        return Ok(RegisterUserHandler.ToProfile(u));
    }

    [HttpPut("photo")]
    public async Task<ActionResult<UserProfileDto>> UpdatePhoto(IFormFile photo, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(User.GetUserId(), ct)
            ?? throw new NotFoundException("user_not_found", "User not found.");
        await using var s = photo.OpenReadStream();
        var p = await _photoStorage.StoreAsync(u.Id, s, photo.ContentType, ct);
        u.AttachPhoto(p, _clock.UtcNow);
        _users.Update(u);
        await _uow.SaveChangesAsync(ct);
        return Ok(RegisterUserHandler.ToProfile(u));
    }

    [HttpDelete("photo")]
    public async Task<IActionResult> DeletePhoto(CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(User.GetUserId(), ct)
            ?? throw new NotFoundException("user_not_found", "User not found.");
        u.RemovePhoto(_clock.UtcNow);
        _users.Update(u);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = PolicyNames.RequireUser)]
[EnableRateLimiting("user")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    public UsersController(IUserRepository users) => _users = users;

    [HttpGet("{id:guid}/photo")]
    public async Task<IActionResult> GetPhoto(Guid id, CancellationToken ct)
    {
        var requester = User.GetUserId();
        var role = User.FindFirst("role")?.Value;
        if (id != requester && !string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }
        var u = await _users.GetByIdAsync(id, ct);
        if (u?.Photo is null) return NotFound();
        var etag = $"\"{u.Photo.Id}\"";
        Response.Headers["Cache-Control"] = "private, max-age=300";
        Response.Headers["ETag"] = etag;
        if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm == etag)
        {
            return StatusCode(304);
        }
        return File(u.Photo.Data, u.Photo.ContentType);
    }
}
