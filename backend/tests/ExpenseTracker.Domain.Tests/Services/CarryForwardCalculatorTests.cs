using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Services;
using ExpenseTracker.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Domain.Tests.Services;

public class CarryForwardCalculatorTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Recompute_FromFirstMonth_SeedsClosingBalance()
    {
        var entries = new FakeEntryRepository();
        var summaries = new FakeSummaryRepository();

        entries.Seed(MakeEntry(2026, 6, 1, EntryType.Income, 2500m));
        entries.Seed(MakeEntry(2026, 6, 10, EntryType.Expense, 500m));

        var calc = new CarryForwardCalculator(entries, summaries, new FixedClock(Now));

        await calc.RecomputeAsync(UserId, "USD", new MonthYear(2026, 6), CancellationToken.None);

        var june = summaries.Get(2026, 6);
        june.Should().NotBeNull();
        june!.OpeningBalance.Should().Be(0m);
        june.TotalIncome.Should().Be(2500m);
        june.TotalExpense.Should().Be(500m);
        june.ClosingBalance.Should().Be(2000m);
    }

    [Fact]
    public async Task Recompute_ChainsAcrossMultipleMonths()
    {
        var entries = new FakeEntryRepository();
        var summaries = new FakeSummaryRepository();

        entries.Seed(MakeEntry(2026, 6, 1, EntryType.Income, 1000m));
        entries.Seed(MakeEntry(2026, 7, 1, EntryType.Expense, 300m));
        entries.Seed(MakeEntry(2026, 8, 1, EntryType.Income, 500m));

        var calc = new CarryForwardCalculator(entries, summaries, new FixedClock(Now));

        await calc.RecomputeAsync(UserId, "USD", new MonthYear(2026, 6), CancellationToken.None);

        summaries.Get(2026, 6)!.ClosingBalance.Should().Be(1000m);
        summaries.Get(2026, 7)!.OpeningBalance.Should().Be(1000m);
        summaries.Get(2026, 7)!.ClosingBalance.Should().Be(700m);
        summaries.Get(2026, 8)!.OpeningBalance.Should().Be(700m);
        summaries.Get(2026, 8)!.ClosingBalance.Should().Be(1200m);
    }

    [Fact]
    public async Task Recompute_UpdatesExistingSummary_PreservingChain()
    {
        var entries = new FakeEntryRepository();
        var summaries = new FakeSummaryRepository();

        entries.Seed(MakeEntry(2026, 6, 1, EntryType.Income, 1000m));
        entries.Seed(MakeEntry(2026, 7, 1, EntryType.Income, 200m));

        var calc = new CarryForwardCalculator(entries, summaries, new FixedClock(Now));
        await calc.RecomputeAsync(UserId, "USD", new MonthYear(2026, 6), CancellationToken.None);
        summaries.Get(2026, 7)!.OpeningBalance.Should().Be(1000m);

        // Add another June entry, recompute — July opening must shift.
        entries.Seed(MakeEntry(2026, 6, 5, EntryType.Income, 500m));
        await calc.RecomputeAsync(UserId, "USD", new MonthYear(2026, 6), CancellationToken.None);

        summaries.Get(2026, 6)!.ClosingBalance.Should().Be(1500m);
        summaries.Get(2026, 7)!.OpeningBalance.Should().Be(1500m);
        summaries.Get(2026, 7)!.ClosingBalance.Should().Be(1700m);
    }

    [Fact]
    public async Task Recompute_FromMiddleMonth_UsesPreviousSummaryOpening()
    {
        var entries = new FakeEntryRepository();
        var summaries = new FakeSummaryRepository();

        // Seed a pre-existing May summary with closing 300, no May entries.
        summaries.Seed(MakeSummary(2026, 5, opening: 0m, income: 800m, expense: 500m));
        entries.Seed(MakeEntry(2026, 6, 1, EntryType.Expense, 100m));

        var calc = new CarryForwardCalculator(entries, summaries, new FixedClock(Now));

        await calc.RecomputeAsync(UserId, "USD", new MonthYear(2026, 6), CancellationToken.None);

        var june = summaries.Get(2026, 6)!;
        june.OpeningBalance.Should().Be(300m);
        june.ClosingBalance.Should().Be(200m);
    }

    // ---------- helpers ----------

    private static Entry MakeEntry(int year, int month, int day, EntryType type, decimal amount) =>
        Entry.Create(
            UserId,
            new DateOnly(year, month, day),
            type,
            amount,
            "USD",
            categoryId: null,
            categoryFreeText: type == EntryType.Income ? "Salary" : "Food",
            note: null,
            linkedCategoryType: null,
            currentMonthForUser: new MonthYear(year, month),
            nowUtc: Now);

    private static MonthlySummary MakeSummary(int year, int month, decimal opening, decimal income, decimal expense) =>
        MonthlySummary.Create(UserId, new MonthYear(year, month), opening, income, expense, 0m, StatusColor.Green, "USD", Now);

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime UtcNow { get; } = now;
    }

    private sealed class FakeEntryRepository : IEntryRepository
    {
        private readonly List<Entry> _store = new();

        public void Seed(Entry entry) => _store.Add(entry);

        public Task<Entry?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_store.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<Entry>> ListByMonthAsync(Guid userId, int year, int month, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Entry>>(_store
                .Where(e => e.UserId == userId && e.EntryDate.Year == year && e.EntryDate.Month == month)
                .ToList());

        public Task<IReadOnlyList<Entry>> ListByUserSinceAsync(Guid userId, int year, int month, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Entry>>(_store
                .Where(e => e.UserId == userId && (e.EntryDate.Year > year || (e.EntryDate.Year == year && e.EntryDate.Month >= month)))
                .ToList());

        public Task<MonthYear?> GetEarliestEntryMonthAsync(Guid userId, CancellationToken ct)
        {
            var first = _store.Where(e => e.UserId == userId).OrderBy(e => e.EntryDate).FirstOrDefault();
            return Task.FromResult<MonthYear?>(first is null ? null : new MonthYear(first.EntryDate.Year, first.EntryDate.Month));
        }

        public Task<MonthYear?> GetLatestEntryMonthAsync(Guid userId, CancellationToken ct)
        {
            var last = _store.Where(e => e.UserId == userId).OrderByDescending(e => e.EntryDate).FirstOrDefault();
            return Task.FromResult<MonthYear?>(last is null ? null : new MonthYear(last.EntryDate.Year, last.EntryDate.Month));
        }

        public Task AddAsync(Entry entry, CancellationToken ct) { _store.Add(entry); return Task.CompletedTask; }
        public void Update(Entry entry) { }
        public void Remove(Entry entry) => _store.Remove(entry);
    }

    private sealed class FakeSummaryRepository : IMonthlySummaryRepository
    {
        private readonly Dictionary<(int Year, int Month), MonthlySummary> _store = new();

        public void Seed(MonthlySummary summary) => _store[(summary.Year, summary.Month)] = summary;

        public MonthlySummary? Get(int year, int month) =>
            _store.TryGetValue((year, month), out var s) ? s : null;

        public Task<MonthlySummary?> GetAsync(Guid userId, int year, int month, CancellationToken ct) =>
            Task.FromResult(Get(year, month));

        public Task<MonthlySummary?> GetLatestAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult(_store.Values.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).FirstOrDefault());

        public Task<IReadOnlyList<MonthlySummary>> ListLastAsync(Guid userId, int monthsBack, int currentYear, int currentMonth, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MonthlySummary>>(_store.Values
                .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
                .Take(monthsBack).ToList());

        public Task AddAsync(MonthlySummary summary, CancellationToken ct) { _store[(summary.Year, summary.Month)] = summary; return Task.CompletedTask; }
        public void Update(MonthlySummary summary) { _store[(summary.Year, summary.Month)] = summary; }
    }
}
