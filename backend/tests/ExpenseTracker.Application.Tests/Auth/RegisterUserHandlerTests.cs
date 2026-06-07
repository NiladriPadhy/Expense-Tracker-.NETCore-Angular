using ExpenseTracker.Application.Auth;
using ExpenseTracker.Application.Auth.Dtos;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Application.Tests.Auth;

public class RegisterUserHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Register_HappyPath_PersistsUserAndReturnsTokens()
    {
        var fakes = new HandlerFakes();
        fakes.Currencies.Add(Currency.Create("USD", "US Dollar", "$", Now));
        var handler = fakes.BuildRegister();

        var req = new RegisterUserRequest("Jane", "jane@example.com", "+15551234567", "S3cret!!", "USD");

        var result = await handler.HandleAsync(req, photo: null, photoContentType: null, ct: CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh-plain");
        result.User.Email.Should().Be("jane@example.com");
        fakes.Users.Added.Should().HaveCount(1);
        fakes.RefreshTokens.Added.Should().HaveCount(1);
        fakes.Uow.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        var fakes = new HandlerFakes();
        fakes.Currencies.Add(Currency.Create("USD", "US Dollar", "$", Now));
        fakes.Users.RegisteredEmails.Add("dup@example.com");
        var handler = fakes.BuildRegister();

        var req = new RegisterUserRequest("Jane", "dup@example.com", "+15551234567", "S3cret!!", "USD");

        var act = () => handler.HandleAsync(req, null, null, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("email_already_registered");
    }

    [Fact]
    public async Task Register_DuplicatePhone_ThrowsConflict()
    {
        var fakes = new HandlerFakes();
        fakes.Currencies.Add(Currency.Create("USD", "US Dollar", "$", Now));
        fakes.Users.RegisteredPhones.Add("+15551234567");
        var handler = fakes.BuildRegister();

        var req = new RegisterUserRequest("Jane", "jane@example.com", "+15551234567", "S3cret!!", "USD");

        var ex = await handler.Invoking(h => h.HandleAsync(req, null, null, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("phone_already_registered");
    }

    [Fact]
    public async Task Register_UnknownCurrency_ThrowsAppException()
    {
        var fakes = new HandlerFakes();
        var handler = fakes.BuildRegister();

        var req = new RegisterUserRequest("Jane", "jane@example.com", "+15551234567", "S3cret!!", "ZZZ");

        var ex = await handler.Invoking(h => h.HandleAsync(req, null, null, CancellationToken.None))
            .Should().ThrowAsync<AppException>();
        ex.Which.Code.Should().Be("invalid_currency");
    }

    [Fact]
    public async Task Register_InactiveCurrency_ThrowsAppException()
    {
        var fakes = new HandlerFakes();
        var c = Currency.Create("USD", "US Dollar", "$", Now);
        c.Deactivate(Now);
        fakes.Currencies.Add(c);
        var handler = fakes.BuildRegister();

        var req = new RegisterUserRequest("Jane", "jane@example.com", "+15551234567", "S3cret!!", "USD");

        var ex = await handler.Invoking(h => h.HandleAsync(req, null, null, CancellationToken.None))
            .Should().ThrowAsync<AppException>();
        ex.Which.Code.Should().Be("invalid_currency");
    }
}

public class LoginUserHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var fakes = new HandlerFakes();
        var user = User.Create("Jane", "jane@example.com", "+15551234567", "hash:S3cret!!", "USD", Now);
        fakes.Users.Store[user.Id] = user;
        fakes.Users.IndexByEmailOrPhone["jane@example.com"] = user;

        var handler = fakes.BuildLogin();
        var result = await handler.HandleAsync(new LoginRequest("jane@example.com", "S3cret!!"), CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Login_UnknownIdentifier_ThrowsUnauthorized()
    {
        var fakes = new HandlerFakes();
        var handler = fakes.BuildLogin();

        var ex = await handler.Invoking(h => h.HandleAsync(new LoginRequest("nobody@example.com", "x"), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        var fakes = new HandlerFakes();
        var user = User.Create("Jane", "jane@example.com", "+15551234567", "hash:S3cret!!", "USD", Now);
        fakes.Users.IndexByEmailOrPhone["jane@example.com"] = user;

        var handler = fakes.BuildLogin();

        var ex = await handler.Invoking(h => h.HandleAsync(new LoginRequest("jane@example.com", "WRONG"), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Login_DeactivatedUser_ThrowsUnauthorized()
    {
        var fakes = new HandlerFakes();
        var user = User.Create("Jane", "jane@example.com", "+15551234567", "hash:S3cret!!", "USD", Now);
        user.Deactivate(Now);
        fakes.Users.IndexByEmailOrPhone["jane@example.com"] = user;

        var handler = fakes.BuildLogin();

        var ex = await handler.Invoking(h => h.HandleAsync(new LoginRequest("jane@example.com", "S3cret!!"), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("invalid_credentials");
    }
}

// ---------- shared fakes (kept internal to this file) ----------

internal sealed class HandlerFakes
{
    public FakeUserRepository Users { get; } = new();
    public FakeCurrencyRepository Currencies { get; } = new();
    public FakePasswordHasher Hasher { get; } = new();
    public FakeTokenService Tokens { get; } = new();
    public FakeRefreshTokenRepository RefreshTokens { get; } = new();
    public FakePhotoStorage Photos { get; } = new();
    public FakeUnitOfWork Uow { get; } = new();
    public FixedClock Clock { get; } = new(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

    public RegisterUserHandler BuildRegister() =>
        new(Users, Currencies, Hasher, Tokens, RefreshTokens, Photos, Uow, Clock);

    public LoginUserHandler BuildLogin() =>
        new(Users, Hasher, Tokens, RefreshTokens, Uow, Clock);
}

internal sealed class FixedClock(DateTime now) : IClock
{
    public DateTime UtcNow { get; } = now;
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;
    public bool Verify(string password, string hash) => hash == "hash:" + password;
}

internal sealed class FakeTokenService : ITokenService
{
    public (string AccessToken, DateTime ExpiresAtUtc) IssueAccessToken(User user) =>
        ("access", new DateTime(2026, 6, 15, 13, 0, 0, DateTimeKind.Utc));

    public (string PlainRefreshToken, string TokenHash, DateTime ExpiresAtUtc) IssueRefreshToken(User user) =>
        ("refresh-plain", "refresh-hash", new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));

    public string HashRefreshToken(string plain) => plain + "-hash";
}

internal sealed class FakeUserRepository : IUserRepository
{
    public Dictionary<Guid, User> Store { get; } = new();
    public Dictionary<string, User> IndexByEmailOrPhone { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> RegisteredEmails { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> RegisteredPhones { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<User> Added { get; } = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Store.GetValueOrDefault(id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        Task.FromResult(Store.Values.FirstOrDefault(u => u.Email == email.ToLowerInvariant()));

    public Task<User?> GetByPhoneAsync(string phone, CancellationToken ct) =>
        Task.FromResult(Store.Values.FirstOrDefault(u => u.Phone == phone));

    public Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken ct) =>
        Task.FromResult(IndexByEmailOrPhone.GetValueOrDefault(identifier));

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        Task.FromResult(RegisteredEmails.Contains(email));

    public Task<bool> PhoneExistsAsync(string phone, CancellationToken ct) =>
        Task.FromResult(RegisteredPhones.Contains(phone));

    public Task<(IReadOnlyList<User> Items, int Total)> ListAsync(string? search, int page, int pageSize, CancellationToken ct) =>
        Task.FromResult<(IReadOnlyList<User>, int)>((Store.Values.ToList(), Store.Count));

    public Task<int> CountActiveAdminsAsync(CancellationToken ct) =>
        Task.FromResult(Store.Values.Count(u => u.Role == UserRole.Admin && u.IsActive && !u.IsDeleted));

    public Task AddAsync(User user, CancellationToken ct) { Added.Add(user); Store[user.Id] = user; return Task.CompletedTask; }
    public void Update(User user) => Store[user.Id] = user;
    public void Remove(User user) => Store.Remove(user.Id);
}

internal sealed class FakeCurrencyRepository : ICurrencyRepository
{
    public Dictionary<string, Currency> Store { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Add(Currency c) => Store[c.Code] = c;

    public Task<Currency?> GetByCodeAsync(string code, CancellationToken ct) =>
        Task.FromResult(Store.GetValueOrDefault(code));

    public Task<IReadOnlyList<Currency>> ListActiveAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Currency>>(Store.Values.Where(c => c.IsActive).ToList());

    public Task<IReadOnlyList<Currency>> ListAllAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Currency>>(Store.Values.ToList());

    public Task<bool> IsReferencedByAnyUserAsync(string code, CancellationToken ct) => Task.FromResult(false);

    public Task AddAsync(Currency currency, CancellationToken ct) { Store[currency.Code] = currency; return Task.CompletedTask; }
    public void Update(Currency currency) => Store[currency.Code] = currency;
    public void Remove(Currency currency) => Store.Remove(currency.Code);
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Added { get; } = new();
    public Dictionary<string, RefreshToken> ByHash { get; } = new();

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
        Task.FromResult(ByHash.GetValueOrDefault(tokenHash));

    public Task AddAsync(RefreshToken token, CancellationToken ct) { Added.Add(token); ByHash[token.TokenHash] = token; return Task.CompletedTask; }

    public Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FakePhotoStorage : IPhotoStorage
{
    public Task<UserProfilePhoto> StoreAsync(Guid userId, Stream content, string contentType, CancellationToken ct) =>
        throw new NotImplementedException("Tests don't exercise photo path.");
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken ct) { SaveCount++; return Task.FromResult(0); }
    public Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct) =>
        Task.FromResult<IAsyncDisposable>(new NullScope());

    private sealed class NullScope : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
