using ExpenseTracker.Application.Auth.Dtos;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Auth;

public sealed class LoginUserHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IRefreshTokenRepository _refresh;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public LoginUserHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        ITokenService tokens,
        IRefreshTokenRepository refresh,
        IUnitOfWork uow,
        IClock clock)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
        _refresh = refresh;
        _uow = uow;
        _clock = clock;
    }

    public async Task<AuthResult> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _users.GetByEmailOrPhoneAsync(request.Identifier, ct).ConfigureAwait(false);
        if (user is null || user.IsDeleted || !user.IsActive ||
            !_hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("invalid_credentials", "Invalid credentials.");
        }

        var (access, accessExp) = _tokens.IssueAccessToken(user);
        var (plain, hash, refreshExp) = _tokens.IssueRefreshToken(user);
        await _refresh.AddAsync(RefreshToken.Issue(user.Id, hash, refreshExp, _clock.UtcNow), ct)
            .ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return new AuthResult(access, plain, accessExp, RegisterUserHandler.ToProfile(user));
    }
}

public sealed class RefreshTokenHandler
{
    private readonly IRefreshTokenRepository _refresh;
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public RefreshTokenHandler(
        IRefreshTokenRepository refresh,
        IUserRepository users,
        ITokenService tokens,
        IUnitOfWork uow,
        IClock clock)
    {
        _refresh = refresh;
        _users = users;
        _tokens = tokens;
        _uow = uow;
        _clock = clock;
    }

    public async Task<AuthResult> HandleAsync(RefreshRequest request, CancellationToken ct)
    {
        var hash = _tokens.HashRefreshToken(request.RefreshToken);
        var existing = await _refresh.GetByHashAsync(hash, ct).ConfigureAwait(false);
        if (existing is null || !existing.IsActive)
        {
            throw new UnauthorizedException("invalid_refresh_token", "Refresh token invalid or expired.");
        }
        var user = await _users.GetByIdAsync(existing.UserId, ct).ConfigureAwait(false);
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            throw new UnauthorizedException("invalid_refresh_token", "User no longer eligible.");
        }

        var (access, accessExp) = _tokens.IssueAccessToken(user);
        var (newPlain, newHash, newExp) = _tokens.IssueRefreshToken(user);
        var newToken = RefreshToken.Issue(user.Id, newHash, newExp, _clock.UtcNow);
        await _refresh.AddAsync(newToken, ct).ConfigureAwait(false);
        existing.Revoke(_clock.UtcNow, newToken.Id);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return new AuthResult(access, newPlain, accessExp, RegisterUserHandler.ToProfile(user));
    }
}

public sealed class LogoutHandler
{
    private readonly IRefreshTokenRepository _refresh;
    private readonly ITokenService _tokens;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public LogoutHandler(IRefreshTokenRepository refresh, ITokenService tokens, IUnitOfWork uow, IClock clock)
    {
        _refresh = refresh;
        _tokens = tokens;
        _uow = uow;
        _clock = clock;
    }

    public async Task HandleAsync(string refreshToken, CancellationToken ct)
    {
        var hash = _tokens.HashRefreshToken(refreshToken);
        var existing = await _refresh.GetByHashAsync(hash, ct).ConfigureAwait(false);
        existing?.Revoke(_clock.UtcNow);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
