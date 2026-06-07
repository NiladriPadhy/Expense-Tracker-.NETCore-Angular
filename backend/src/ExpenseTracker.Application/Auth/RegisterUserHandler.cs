using ExpenseTracker.Application.Auth.Dtos;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Auth;

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _users;
    private readonly ICurrencyRepository _currencies;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IRefreshTokenRepository _refresh;
    private readonly IPhotoStorage _photoStorage;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public RegisterUserHandler(
        IUserRepository users,
        ICurrencyRepository currencies,
        IPasswordHasher hasher,
        ITokenService tokens,
        IRefreshTokenRepository refresh,
        IPhotoStorage photoStorage,
        IUnitOfWork uow,
        IClock clock)
    {
        _users = users;
        _currencies = currencies;
        _hasher = hasher;
        _tokens = tokens;
        _refresh = refresh;
        _photoStorage = photoStorage;
        _uow = uow;
        _clock = clock;
    }

    public async Task<AuthResult> HandleAsync(
        RegisterUserRequest request,
        Stream? photo,
        string? photoContentType,
        CancellationToken ct)
    {
        if (await _users.EmailExistsAsync(request.Email, ct).ConfigureAwait(false))
        {
            throw new ConflictException("email_already_registered", "Email is already registered.");
        }
        if (await _users.PhoneExistsAsync(request.Phone, ct).ConfigureAwait(false))
        {
            throw new ConflictException("phone_already_registered", "Phone is already registered.");
        }
        var currency = await _currencies.GetByCodeAsync(request.CurrencyCode, ct).ConfigureAwait(false);
        if (currency is null || !currency.IsActive)
        {
            throw new AppException("invalid_currency", "Currency is not available.");
        }

        var user = User.Create(
            request.FullName,
            request.Email,
            request.Phone,
            _hasher.Hash(request.Password),
            currency.Code,
            _clock.UtcNow);

        if (photo is not null && !string.IsNullOrWhiteSpace(photoContentType))
        {
            var p = await _photoStorage.StoreAsync(user.Id, photo, photoContentType, ct).ConfigureAwait(false);
            user.AttachPhoto(p, _clock.UtcNow);
        }

        await _users.AddAsync(user, ct).ConfigureAwait(false);

        var (access, accessExp) = _tokens.IssueAccessToken(user);
        var (plain, hash, refreshExp) = _tokens.IssueRefreshToken(user);
        await _refresh.AddAsync(RefreshToken.Issue(user.Id, hash, refreshExp, _clock.UtcNow), ct)
            .ConfigureAwait(false);

        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return new AuthResult(access, plain, accessExp, ToProfile(user));
    }

    public static UserProfileDto ToProfile(User u) => new(
        u.Id, u.FullName, u.Email, u.Phone, u.CurrencyCode, u.Role.ToString(), u.PhotoId is not null);
}
