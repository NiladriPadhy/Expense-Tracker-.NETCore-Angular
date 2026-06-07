using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return new NullAsyncDisposable();
        }
        var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        return new EfTransaction(tx);
    }

    private sealed class EfTransaction : IAsyncDisposable
    {
        private readonly IDbContextTransaction _tx;
        private bool _committed;
        public EfTransaction(IDbContextTransaction tx) => _tx = tx;

        public async ValueTask DisposeAsync()
        {
            if (!_committed)
            {
                try { await _tx.CommitAsync().ConfigureAwait(false); _committed = true; }
                catch { await _tx.RollbackAsync().ConfigureAwait(false); throw; }
            }
            await _tx.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class NullAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Users.Include(u => u.Photo).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var e = email.Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(u => u.Email == e, ct);
    }

    public Task<User?> GetByPhoneAsync(string phone, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(u => u.Phone == phone, ct);

    public Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken ct)
    {
        var t = identifier.Trim();
        if (t.Contains('@', StringComparison.Ordinal))
        {
            var e = t.ToLowerInvariant();
            return _db.Users.FirstOrDefaultAsync(u => u.Email == e, ct);
        }
        return _db.Users.FirstOrDefaultAsync(u => u.Phone == t, ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        var e = email.Trim().ToLowerInvariant();
        return _db.Users.AnyAsync(u => u.Email == e, ct);
    }

    public Task<bool> PhoneExistsAsync(string phone, CancellationToken ct)
        => _db.Users.AnyAsync(u => u.Phone == phone, ct);

    public async Task<(IReadOnlyList<User> Items, int Total)> ListAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        IQueryable<User> q = _db.Users.AsNoTracking().Where(u => !u.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(u =>
                u.Email.Contains(s) ||
                u.Phone.Contains(s) ||
                u.FullName.ToLower().Contains(s));
        }
        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return (items, total);
    }

    public Task<int> CountActiveAdminsAsync(CancellationToken ct)
        => _db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && !u.IsDeleted, ct);

    public Task AddAsync(User user, CancellationToken ct) => _db.Users.AddAsync(user, ct).AsTask();
    public void Update(User user) => _db.Users.Update(user);
    public void Remove(User user) => _db.Users.Remove(user);
}

public sealed class CurrencyRepository : ICurrencyRepository
{
    private readonly AppDbContext _db;
    public CurrencyRepository(AppDbContext db) => _db = db;

    public Task<Currency?> GetByCodeAsync(string code, CancellationToken ct)
    {
        var c = code.Trim().ToUpperInvariant();
        return _db.Currencies.FirstOrDefaultAsync(x => x.Code == c, ct);
    }

    public Task<IReadOnlyList<Currency>> ListActiveAsync(CancellationToken ct)
        => _db.Currencies.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync(ct)
            .ContinueWith<IReadOnlyList<Currency>>(t => t.Result, ct);

    public Task<IReadOnlyList<Currency>> ListAllAsync(CancellationToken ct)
        => _db.Currencies.AsNoTracking().OrderBy(c => c.Code).ToListAsync(ct)
            .ContinueWith<IReadOnlyList<Currency>>(t => t.Result, ct);

    public Task<bool> IsReferencedByAnyUserAsync(string code, CancellationToken ct)
    {
        var c = code.Trim().ToUpperInvariant();
        return _db.Users.AnyAsync(u => u.CurrencyCode == c, ct);
    }

    public Task AddAsync(Currency currency, CancellationToken ct) => _db.Currencies.AddAsync(currency, ct).AsTask();
    public void Update(Currency currency) => _db.Currencies.Update(currency);
    public void Remove(Currency currency) => _db.Currencies.Remove(currency);
}

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;
    public CategoryRepository(AppDbContext db) => _db = db;

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<IReadOnlyList<Category>> ListActiveAsync(EntryType? type, CancellationToken ct)
    {
        IQueryable<Category> q = _db.Categories.AsNoTracking().Where(c => c.IsActive);
        if (type is not null)
        {
            q = q.Where(c => c.Type == type);
        }
        return q.OrderBy(c => c.Type).ThenBy(c => c.Name)
            .ToListAsync(ct).ContinueWith<IReadOnlyList<Category>>(t => t.Result, ct);
    }

    public Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken ct)
        => _db.Categories.AsNoTracking().OrderBy(c => c.Type).ThenBy(c => c.Name)
            .ToListAsync(ct).ContinueWith<IReadOnlyList<Category>>(t => t.Result, ct);

    public Task<bool> NameExistsAsync(string name, EntryType type, Guid? excludingId, CancellationToken ct)
    {
        var n = name.Trim();
        return _db.Categories.AnyAsync(
            c => c.Type == type && c.Name == n && (excludingId == null || c.Id != excludingId),
            ct);
    }

    public Task AddAsync(Category category, CancellationToken ct) => _db.Categories.AddAsync(category, ct).AsTask();
    public void Update(Category category) => _db.Categories.Update(category);
}

public sealed class EntryRepository : IEntryRepository
{
    private readonly AppDbContext _db;
    public EntryRepository(AppDbContext db) => _db = db;

    public Task<Entry?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Entries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<IReadOnlyList<Entry>> ListByMonthAsync(Guid userId, int year, int month, CancellationToken ct)
        => _db.Entries.AsNoTracking()
            .Where(e => e.UserId == userId && e.EntryDate.Year == year && e.EntryDate.Month == month)
            .OrderBy(e => e.EntryDate).ThenBy(e => e.CreatedAtUtc)
            .ToListAsync(ct).ContinueWith<IReadOnlyList<Entry>>(t => t.Result, ct);

    public Task<IReadOnlyList<Entry>> ListByUserSinceAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var cutoff = new DateOnly(year, month, 1);
        return _db.Entries.AsNoTracking()
            .Where(e => e.UserId == userId && e.EntryDate >= cutoff)
            .OrderBy(e => e.EntryDate)
            .ToListAsync(ct).ContinueWith<IReadOnlyList<Entry>>(t => t.Result, ct);
    }

    public async Task<MonthYear?> GetEarliestEntryMonthAsync(Guid userId, CancellationToken ct)
    {
        var d = await _db.Entries.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (DateOnly?)e.EntryDate)
            .MinAsync(ct).ConfigureAwait(false);
        return d is null ? null : new MonthYear(d.Value.Year, d.Value.Month);
    }

    public async Task<MonthYear?> GetLatestEntryMonthAsync(Guid userId, CancellationToken ct)
    {
        var d = await _db.Entries.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (DateOnly?)e.EntryDate)
            .MaxAsync(ct).ConfigureAwait(false);
        return d is null ? null : new MonthYear(d.Value.Year, d.Value.Month);
    }

    public Task AddAsync(Entry entry, CancellationToken ct) => _db.Entries.AddAsync(entry, ct).AsTask();
    public void Update(Entry entry) => _db.Entries.Update(entry);
    public void Remove(Entry entry) => _db.Entries.Remove(entry);
}

public sealed class MonthlySummaryRepository : IMonthlySummaryRepository
{
    private readonly AppDbContext _db;
    public MonthlySummaryRepository(AppDbContext db) => _db = db;

    public Task<MonthlySummary?> GetAsync(Guid userId, int year, int month, CancellationToken ct)
        => _db.MonthlySummaries.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Year == year && s.Month == month, ct);

    public Task<MonthlySummary?> GetLatestAsync(Guid userId, CancellationToken ct)
        => _db.MonthlySummaries.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
            .FirstOrDefaultAsync(ct);

    public Task<IReadOnlyList<MonthlySummary>> ListLastAsync(Guid userId, int monthsBack, int currentYear, int currentMonth, CancellationToken ct)
    {
        var end = new MonthYear(currentYear, currentMonth);
        var start = end;
        for (var i = 0; i < monthsBack - 1; i++) { start = start.Previous(); }

        return _db.MonthlySummaries.AsNoTracking()
            .Where(s => s.UserId == userId &&
                (s.Year > start.Year || (s.Year == start.Year && s.Month >= start.Month)) &&
                (s.Year < end.Year || (s.Year == end.Year && s.Month <= end.Month)))
            .OrderBy(s => s.Year).ThenBy(s => s.Month)
            .ToListAsync(ct).ContinueWith<IReadOnlyList<MonthlySummary>>(t => t.Result, ct);
    }

    public Task AddAsync(MonthlySummary summary, CancellationToken ct) => _db.MonthlySummaries.AddAsync(summary, ct).AsTask();
    public void Update(MonthlySummary summary) => _db.MonthlySummaries.Update(summary);
}

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
        => _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task AddAsync(RefreshToken token, CancellationToken ct) => _db.RefreshTokens.AddAsync(token, ct).AsTask();

    public async Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(ct).ConfigureAwait(false);
        foreach (var t in active)
        {
            t.Revoke(nowUtc);
        }
    }
}
