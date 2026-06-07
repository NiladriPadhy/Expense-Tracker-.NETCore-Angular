using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExpenseTracker.Infrastructure.Persistence.Seeding;

public sealed class Seeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly IConfiguration _config;

    public Seeder(AppDbContext db, IPasswordHasher hasher, IClock clock, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        _config = config;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct).ConfigureAwait(false);
        await SeedCurrenciesAsync(ct).ConfigureAwait(false);
        await SeedCategoriesAsync(ct).ConfigureAwait(false);
        await SeedAdminAsync(ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task SeedCurrenciesAsync(CancellationToken ct)
    {
        if (await _db.Currencies.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }
        var now = _clock.UtcNow;
        var seed = new[]
        {
            Currency.Create("USD", "US Dollar", "$", now),
            Currency.Create("EUR", "Euro", "\u20AC", now),
            Currency.Create("GBP", "British Pound", "\u00A3", now),
            Currency.Create("INR", "Indian Rupee", "\u20B9", now),
            Currency.Create("JPY", "Japanese Yen", "\u00A5", now),
        };
        await _db.Currencies.AddRangeAsync(seed, ct).ConfigureAwait(false);
    }

    private async Task SeedCategoriesAsync(CancellationToken ct)
    {
        if (await _db.Categories.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }
        var now = _clock.UtcNow;
        var expenses = new[]
        {
            "Food", "Transport", "Housing", "Utilities", "Healthcare",
            "Entertainment", "Shopping", "Education", "Other",
        }.Select(n => Category.Create(n, EntryType.Expense, now));
        var incomes = new[]
        {
            "Salary", "Bonus", "Interest", "Gift", "Other",
        }.Select(n => Category.Create(n, EntryType.Income, now));
        await _db.Categories.AddRangeAsync(expenses.Concat(incomes), ct).ConfigureAwait(false);
    }

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Role == UserRole.Admin, ct).ConfigureAwait(false))
        {
            return;
        }
        var email = _config["Seed:DefaultAdminEmail"];
        var password = _config["Seed:DefaultAdminPassword"];
        var phone = _config["Seed:DefaultAdminPhone"] ?? "+10000000000";
        var currency = _config["Seed:DefaultAdminCurrency"] ?? "USD";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return; // Do not seed without explicit configuration.
        }

        var admin = User.Create(
            fullName: _config["Seed:DefaultAdminName"] ?? "Administrator",
            email: email,
            phone: phone,
            passwordHash: _hasher.Hash(password),
            currencyCode: currency,
            nowUtc: _clock.UtcNow,
            role: UserRole.Admin);
        await _db.Users.AddAsync(admin, ct).ConfigureAwait(false);
    }
}
