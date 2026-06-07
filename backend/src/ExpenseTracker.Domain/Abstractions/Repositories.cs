using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Domain.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByPhoneAsync(string phone, CancellationToken ct);
    Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<bool> PhoneExistsAsync(string phone, CancellationToken ct);
    Task<(IReadOnlyList<User> Items, int Total)> ListAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<int> CountActiveAdminsAsync(CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    void Update(User user);
    void Remove(User user);
}

public interface ICurrencyRepository
{
    Task<Currency?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Currency>> ListActiveAsync(CancellationToken ct);
    Task<IReadOnlyList<Currency>> ListAllAsync(CancellationToken ct);
    Task<bool> IsReferencedByAnyUserAsync(string code, CancellationToken ct);
    Task AddAsync(Currency currency, CancellationToken ct);
    void Update(Currency currency);
    void Remove(Currency currency);
}

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Category>> ListActiveAsync(Common.EntryType? type, CancellationToken ct);
    Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken ct);
    Task<bool> NameExistsAsync(string name, Common.EntryType type, Guid? excludingId, CancellationToken ct);
    Task AddAsync(Category category, CancellationToken ct);
    void Update(Category category);
}

public interface IEntryRepository
{
    Task<Entry?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Entry>> ListByMonthAsync(Guid userId, int year, int month, CancellationToken ct);
    Task<IReadOnlyList<Entry>> ListByUserSinceAsync(Guid userId, int year, int month, CancellationToken ct);
    Task<ValueObjects.MonthYear?> GetEarliestEntryMonthAsync(Guid userId, CancellationToken ct);
    Task<ValueObjects.MonthYear?> GetLatestEntryMonthAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Entry entry, CancellationToken ct);
    void Update(Entry entry);
    void Remove(Entry entry);
}

public interface IMonthlySummaryRepository
{
    Task<MonthlySummary?> GetAsync(Guid userId, int year, int month, CancellationToken ct);
    Task<MonthlySummary?> GetLatestAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<MonthlySummary>> ListLastAsync(Guid userId, int monthsBack, int currentYear, int currentMonth, CancellationToken ct);
    Task AddAsync(MonthlySummary summary, CancellationToken ct);
    void Update(MonthlySummary summary);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAtUtc) IssueAccessToken(User user);
    (string PlainRefreshToken, string TokenHash, DateTime ExpiresAtUtc) IssueRefreshToken(User user);
    string HashRefreshToken(string plain);
}

public interface IPhotoStorage
{
    Task<UserProfilePhoto> StoreAsync(Guid userId, Stream content, string contentType, CancellationToken ct);
}

public interface IUserWriteCoordinator
{
    Task<T> RunAsync<T>(Guid userId, Func<CancellationToken, Task<T>> work, CancellationToken ct);
    Task RunAsync(Guid userId, Func<CancellationToken, Task> work, CancellationToken ct);
}
