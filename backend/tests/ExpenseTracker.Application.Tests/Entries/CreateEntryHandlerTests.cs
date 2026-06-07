using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Entries;
using ExpenseTracker.Application.Entries.Dtos;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Application.Tests.Entries;

public class CreateEntryHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly MonthYear CurrentMonth = new(2026, 6);

    [Fact]
    public async Task Create_HappyPath_PersistsAndRecomputes()
    {
        var fakes = EntryFakes.New();
        var user = fakes.SeedUser();
        var cat = Category.Create("Food", EntryType.Expense, Now);
        fakes.Categories.Store[cat.Id] = cat;

        var handler = fakes.BuildCreate();
        var dto = new EntryCreateDto(new DateOnly(2026, 6, 10), EntryType.Expense, 25m, cat.Id, null, null);

        var result = await handler.HandleAsync(user.Id, dto, CancellationToken.None);

        result.Amount.Should().Be(25m);
        result.CategoryName.Should().Be("Food");
        fakes.Entries.Store.Should().HaveCount(1);
        fakes.Summaries.Store.Should().ContainKey((2026, 6));
    }

    [Fact]
    public async Task Create_FreeTextCategory_StoresSnapshot()
    {
        var fakes = EntryFakes.New();
        var user = fakes.SeedUser();
        var handler = fakes.BuildCreate();

        var dto = new EntryCreateDto(new DateOnly(2026, 6, 10), EntryType.Expense, 9.99m, null, "Coffee", "morning");

        var result = await handler.HandleAsync(user.Id, dto, CancellationToken.None);

        result.CategoryName.Should().Be("Coffee");
        result.CategoryId.Should().BeNull();
    }

    [Fact]
    public async Task Create_CategoryTypeMismatch_ThrowsArgument()
    {
        var fakes = EntryFakes.New();
        var user = fakes.SeedUser();
        var incomeCategory = Category.Create("Salary", EntryType.Income, Now);
        fakes.Categories.Store[incomeCategory.Id] = incomeCategory;
        var handler = fakes.BuildCreate();

        var dto = new EntryCreateDto(new DateOnly(2026, 6, 10), EntryType.Expense, 25m, incomeCategory.Id, null, null);

        await handler.Invoking(h => h.HandleAsync(user.Id, dto, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Create_FutureMonth_ThrowsAppExceptionWithCode()
    {
        var fakes = EntryFakes.New();
        var user = fakes.SeedUser();
        var handler = fakes.BuildCreate();

        var dto = new EntryCreateDto(new DateOnly(2026, 12, 1), EntryType.Income, 100m, null, "Bonus", null);

        var ex = await handler.Invoking(h => h.HandleAsync(user.Id, dto, CancellationToken.None))
            .Should().ThrowAsync<AppException>();
        ex.Which.Code.Should().Be("future_month_write_forbidden");
    }

    [Fact]
    public async Task Create_NonPositiveAmount_ThrowsArgumentOutOfRange()
    {
        var fakes = EntryFakes.New();
        var user = fakes.SeedUser();
        var handler = fakes.BuildCreate();

        var dto = new EntryCreateDto(new DateOnly(2026, 6, 10), EntryType.Expense, 0m, null, "Coffee", null);

        await handler.Invoking(h => h.HandleAsync(user.Id, dto, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Create_UnknownUser_ThrowsNotFound()
    {
        var fakes = EntryFakes.New();
        var handler = fakes.BuildCreate();
        var dto = new EntryCreateDto(new DateOnly(2026, 6, 10), EntryType.Expense, 25m, null, "x", null);

        await handler.Invoking(h => h.HandleAsync(Guid.NewGuid(), dto, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}

public class UpdateDeleteEntryHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Update_OtherUser_ThrowsForbidden()
    {
        var fakes = EntryFakes.New();
        var owner = fakes.SeedUser();
        var entry = Entry.Create(owner.Id, new DateOnly(2026, 6, 10), EntryType.Expense, 10m, "USD",
            null, "x", null, null, new MonthYear(2026, 6), Now);
        fakes.Entries.Store.Add(entry);

        var intruder = fakes.SeedUser();
        var handler = fakes.BuildUpdate();
        var dto = new EntryUpdateDto(new DateOnly(2026, 6, 11), EntryType.Expense, 11m, null, "y", null);

        await handler.Invoking(h => h.HandleAsync(intruder.Id, entry.Id, dto, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Delete_OtherUser_ThrowsForbidden()
    {
        var fakes = EntryFakes.New();
        var owner = fakes.SeedUser();
        var entry = Entry.Create(owner.Id, new DateOnly(2026, 6, 10), EntryType.Expense, 10m, "USD",
            null, "x", null, null, new MonthYear(2026, 6), Now);
        fakes.Entries.Store.Add(entry);

        var intruder = fakes.SeedUser();
        var handler = fakes.BuildDelete();

        await handler.Invoking(h => h.HandleAsync(intruder.Id, entry.Id, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Delete_OwnEntry_RemovesAndRecomputes()
    {
        var fakes = EntryFakes.New();
        var owner = fakes.SeedUser();
        var entry = Entry.Create(owner.Id, new DateOnly(2026, 6, 10), EntryType.Income, 100m, "USD",
            null, "x", null, null, new MonthYear(2026, 6), Now);
        fakes.Entries.Store.Add(entry);

        var handler = fakes.BuildDelete();
        await handler.HandleAsync(owner.Id, entry.Id, CancellationToken.None);

        fakes.Entries.Store.Should().BeEmpty();
        fakes.Summaries.Store[(2026, 6)].TotalIncome.Should().Be(0m);
    }
}

public class ListEntriesByMonthHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task List_ReturnsCategoryNameLiveJoin()
    {
        var fakes = EntryFakes.New();
        var owner = fakes.SeedUser();
        var cat = Category.Create("Groceries", EntryType.Expense, Now);
        fakes.Categories.Store[cat.Id] = cat;

        var linked = Entry.Create(owner.Id, new DateOnly(2026, 6, 10), EntryType.Expense, 10m, "USD",
            cat.Id, null, null, EntryType.Expense, new MonthYear(2026, 6), Now);
        var free = Entry.Create(owner.Id, new DateOnly(2026, 6, 11), EntryType.Expense, 5m, "USD",
            null, "Bus fare", null, null, new MonthYear(2026, 6), Now);
        fakes.Entries.Store.Add(linked);
        fakes.Entries.Store.Add(free);

        var handler = new ListEntriesByMonthHandler(fakes.Entries, fakes.Categories);
        var list = await handler.HandleAsync(owner.Id, 2026, 6, CancellationToken.None);

        list.Should().HaveCount(2);
        list.Single(e => e.CategoryId == cat.Id).CategoryName.Should().Be("Groceries");
        list.Single(e => e.CategoryId == null).CategoryName.Should().Be("Bus fare");
    }
}

// ---------- shared fakes for entry tests ----------

internal sealed class EntryFakes
{
    public FakeUserRepoEntries Users { get; } = new();
    public FakeEntryRepo Entries { get; } = new();
    public FakeCategoryRepo Categories { get; } = new();
    public FakeSummaryRepo Summaries { get; } = new();
    public PassthroughCoordinator Coord { get; } = new();
    public FakeUow Uow { get; } = new();
    public StubClock Clock { get; } = new(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

    public static EntryFakes New() => new();

    public User SeedUser()
    {
        var user = User.Create("Jane", $"jane{Guid.NewGuid():N}@x.com", $"+1555{Random.Shared.Next(1000000, 9999999)}", "h", "USD", Clock.UtcNow);
        Users.Store[user.Id] = user;
        return user;
    }

    public CreateEntryHandler BuildCreate() => new(Users, Entries, Categories, Summaries, Coord, Uow, Clock);
    public UpdateEntryHandler BuildUpdate() => new(Users, Entries, Categories, Summaries, Coord, Uow, Clock);
    public DeleteEntryHandler BuildDelete() => new(Users, Entries, Summaries, Coord, Uow, Clock);
}

internal sealed class StubClock(DateTime now) : IClock
{
    public DateTime UtcNow { get; } = now;
}

internal sealed class PassthroughCoordinator : IUserWriteCoordinator
{
    public Task<T> RunAsync<T>(Guid userId, Func<CancellationToken, Task<T>> work, CancellationToken ct) => work(ct);
    public Task RunAsync(Guid userId, Func<CancellationToken, Task> work, CancellationToken ct) => work(ct);
}

internal sealed class FakeUow : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(0);
    public Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct) =>
        Task.FromResult<IAsyncDisposable>(new Null());
    private sealed class Null : IAsyncDisposable { public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
}

internal sealed class FakeUserRepoEntries : IUserRepository
{
    public Dictionary<Guid, User> Store { get; } = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Store.GetValueOrDefault(id));
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => Task.FromResult<User?>(null);
    public Task<User?> GetByPhoneAsync(string phone, CancellationToken ct) => Task.FromResult<User?>(null);
    public Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken ct) => Task.FromResult<User?>(null);
    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> PhoneExistsAsync(string phone, CancellationToken ct) => Task.FromResult(false);
    public Task<(IReadOnlyList<User> Items, int Total)> ListAsync(string? search, int page, int pageSize, CancellationToken ct) =>
        Task.FromResult<(IReadOnlyList<User>, int)>((Array.Empty<User>(), 0));
    public Task<int> CountActiveAdminsAsync(CancellationToken ct) => Task.FromResult(0);
    public Task AddAsync(User user, CancellationToken ct) { Store[user.Id] = user; return Task.CompletedTask; }
    public void Update(User user) => Store[user.Id] = user;
    public void Remove(User user) => Store.Remove(user.Id);
}

internal sealed class FakeEntryRepo : IEntryRepository
{
    public List<Entry> Store { get; } = new();

    public Task<Entry?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Store.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<Entry>> ListByMonthAsync(Guid userId, int year, int month, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Entry>>(Store
            .Where(e => e.UserId == userId && e.EntryDate.Year == year && e.EntryDate.Month == month).ToList());

    public Task<IReadOnlyList<Entry>> ListByUserSinceAsync(Guid userId, int year, int month, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Entry>>(Store
            .Where(e => e.UserId == userId && (e.EntryDate.Year > year || (e.EntryDate.Year == year && e.EntryDate.Month >= month))).ToList());

    public Task<MonthYear?> GetEarliestEntryMonthAsync(Guid userId, CancellationToken ct)
    {
        var first = Store.Where(e => e.UserId == userId).OrderBy(e => e.EntryDate).FirstOrDefault();
        return Task.FromResult<MonthYear?>(first is null ? null : new MonthYear(first.EntryDate.Year, first.EntryDate.Month));
    }

    public Task<MonthYear?> GetLatestEntryMonthAsync(Guid userId, CancellationToken ct)
    {
        var last = Store.Where(e => e.UserId == userId).OrderByDescending(e => e.EntryDate).FirstOrDefault();
        return Task.FromResult<MonthYear?>(last is null ? null : new MonthYear(last.EntryDate.Year, last.EntryDate.Month));
    }

    public Task AddAsync(Entry entry, CancellationToken ct) { Store.Add(entry); return Task.CompletedTask; }
    public void Update(Entry entry) { }
    public void Remove(Entry entry) => Store.Remove(entry);
}

internal sealed class FakeCategoryRepo : ICategoryRepository
{
    public Dictionary<Guid, Category> Store { get; } = new();

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Store.GetValueOrDefault(id));
    public Task<IReadOnlyList<Category>> ListActiveAsync(EntryType? type, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Category>>(Store.Values.Where(c => c.IsActive && (type is null || c.Type == type)).ToList());
    public Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Category>>(Store.Values.ToList());
    public Task<bool> NameExistsAsync(string name, EntryType type, Guid? excludingId, CancellationToken ct) =>
        Task.FromResult(false);
    public Task AddAsync(Category category, CancellationToken ct) { Store[category.Id] = category; return Task.CompletedTask; }
    public void Update(Category category) => Store[category.Id] = category;
}

internal sealed class FakeSummaryRepo : IMonthlySummaryRepository
{
    public Dictionary<(int Year, int Month), MonthlySummary> Store { get; } = new();

    public Task<MonthlySummary?> GetAsync(Guid userId, int year, int month, CancellationToken ct) =>
        Task.FromResult(Store.GetValueOrDefault((year, month)));

    public Task<MonthlySummary?> GetLatestAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult(Store.Values.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).FirstOrDefault());

    public Task<IReadOnlyList<MonthlySummary>> ListLastAsync(Guid userId, int monthsBack, int currentYear, int currentMonth, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<MonthlySummary>>(Store.Values
            .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).Take(monthsBack).ToList());

    public Task AddAsync(MonthlySummary summary, CancellationToken ct) { Store[(summary.Year, summary.Month)] = summary; return Task.CompletedTask; }
    public void Update(MonthlySummary summary) => Store[(summary.Year, summary.Month)] = summary;
}
