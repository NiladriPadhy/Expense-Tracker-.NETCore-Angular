using ExpenseTracker.Application.Auth.Dtos;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Admin.Users;

public sealed record AdminUserDto(
    Guid Id, string FullName, string Email, string Phone, string CurrencyCode,
    string Role, bool IsActive, bool IsDeleted, bool HasPhoto,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record AdminUserUpdateDto(
    string? FullName, string? Phone, string? CurrencyCode, string? Role, bool? IsActive);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed class ListUsersHandler
{
    private readonly IUserRepository _users;
    public ListUsersHandler(IUserRepository users) => _users = users;

    public async Task<PagedResult<AdminUserDto>> HandleAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 25;
        var (items, total) = await _users.ListAsync(search, page, pageSize, ct).ConfigureAwait(false);
        return new PagedResult<AdminUserDto>(items.Select(Map).ToList(), page, pageSize, total);
    }

    public static AdminUserDto Map(User u) => new(
        u.Id, u.FullName, u.Email, u.Phone, u.CurrencyCode,
        u.Role.ToString(), u.IsActive, u.IsDeleted, u.PhotoId is not null,
        u.CreatedAtUtc, u.UpdatedAtUtc);
}

public sealed class GetUserHandler
{
    private readonly IUserRepository _users;
    public GetUserHandler(IUserRepository users) => _users = users;

    public async Task<AdminUserDto> HandleAsync(Guid id, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("user_not_found", "User not found.");
        return ListUsersHandler.Map(u);
    }
}

public sealed class UpdateUserHandler
{
    private readonly IUserRepository _users;
    private readonly ICurrencyRepository _currencies;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public UpdateUserHandler(IUserRepository users, ICurrencyRepository currencies, IUnitOfWork uow, IClock clock)
    {
        _users = users; _currencies = currencies; _uow = uow; _clock = clock;
    }

    public async Task<AdminUserDto> HandleAsync(Guid id, AdminUserUpdateDto dto, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("user_not_found", "User not found.");

        if (!string.IsNullOrWhiteSpace(dto.CurrencyCode))
        {
            var cur = await _currencies.GetByCodeAsync(dto.CurrencyCode, ct).ConfigureAwait(false);
            if (cur is null || !cur.IsActive)
            {
                throw new AppException("invalid_currency", "Currency not available.");
            }
        }

        u.UpdateProfile(dto.FullName ?? u.FullName, dto.Phone ?? u.Phone, dto.CurrencyCode ?? u.CurrencyCode, _clock.UtcNow);

        if (!string.IsNullOrWhiteSpace(dto.Role) && Enum.TryParse<UserRole>(dto.Role, true, out var newRole))
        {
            if (u.Role == UserRole.Admin && newRole != UserRole.Admin)
            {
                var admins = await _users.CountActiveAdminsAsync(ct).ConfigureAwait(false);
                if (admins <= 1)
                {
                    throw new ConflictException("last_admin", "Cannot demote the last active admin.");
                }
            }
            u.ChangeRole(newRole, _clock.UtcNow);
        }

        if (dto.IsActive is { } active)
        {
            if (u.Role == UserRole.Admin && u.IsActive && !active)
            {
                var admins = await _users.CountActiveAdminsAsync(ct).ConfigureAwait(false);
                if (admins <= 1)
                {
                    throw new ConflictException("last_admin", "Cannot deactivate the last active admin.");
                }
            }
            if (active) u.Activate(_clock.UtcNow); else u.Deactivate(_clock.UtcNow);
        }

        _users.Update(u);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return ListUsersHandler.Map(u);
    }
}

public sealed class DeleteUserHandler
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refresh;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public DeleteUserHandler(IUserRepository users, IRefreshTokenRepository refresh, IUnitOfWork uow, IClock clock)
    {
        _users = users; _refresh = refresh; _uow = uow; _clock = clock;
    }

    public async Task HandleAsync(Guid id, bool hard, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("user_not_found", "User not found.");

        if (u.Role == UserRole.Admin)
        {
            var admins = await _users.CountActiveAdminsAsync(ct).ConfigureAwait(false);
            if (admins <= 1)
            {
                throw new ConflictException("last_admin", "Cannot delete the last active admin.");
            }
        }

        await _refresh.RevokeAllForUserAsync(u.Id, _clock.UtcNow, ct).ConfigureAwait(false);

        if (hard)
        {
            _users.Remove(u);
        }
        else
        {
            u.SoftDelete(_clock.UtcNow);
            _users.Update(u);
        }
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
