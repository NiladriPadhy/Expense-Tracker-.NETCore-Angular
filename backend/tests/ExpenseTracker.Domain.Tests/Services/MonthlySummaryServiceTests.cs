using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Services;
using ExpenseTracker.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Domain.Tests.Services;

public class MonthlySummaryServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly MonthYear Current = new(2026, 6);
    private static readonly DateTime Now = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Entry MakeIncome(decimal amount) =>
        Entry.Create(
            UserId,
            new DateOnly(2026, 6, 10),
            EntryType.Income,
            amount,
            "USD",
            categoryId: null,
            categoryFreeText: "Salary",
            note: null,
            linkedCategoryType: null,
            currentMonthForUser: Current,
            nowUtc: Now);

    private static Entry MakeExpense(decimal amount) =>
        Entry.Create(
            UserId,
            new DateOnly(2026, 6, 10),
            EntryType.Expense,
            amount,
            "USD",
            categoryId: null,
            categoryFreeText: "Food",
            note: null,
            linkedCategoryType: null,
            currentMonthForUser: Current,
            nowUtc: Now);

    [Fact]
    public void Compute_NoEntries_ReturnsOpeningAsClosing()
    {
        var result = MonthlySummaryService.Compute(openingBalance: 100m, entries: Array.Empty<Entry>());

        result.OpeningBalance.Should().Be(100m);
        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.ClosingBalance.Should().Be(100m);
        result.SavingsRatePct.Should().Be(0m);
        result.StatusColor.Should().Be(StatusColor.BloodRed);
    }

    [Fact]
    public void Compute_SumsIncomeAndExpense()
    {
        var entries = new[]
        {
            MakeIncome(1000m),
            MakeIncome(500m),
            MakeExpense(300m),
            MakeExpense(200m),
        };

        var result = MonthlySummaryService.Compute(openingBalance: 50m, entries);

        result.TotalIncome.Should().Be(1500m);
        result.TotalExpense.Should().Be(500m);
        result.ClosingBalance.Should().Be(50m + 1500m - 500m);
    }

    [Fact]
    public void Compute_ClassifiesGreen_When30PercentSaved()
    {
        var entries = new[] { MakeIncome(1000m), MakeExpense(700m) };

        var result = MonthlySummaryService.Compute(0m, entries);

        result.SavingsRatePct.Should().Be(30m);
        result.StatusColor.Should().Be(StatusColor.Green);
    }

    [Fact]
    public void Compute_ClassifiesBloodRed_WhenExpenseEqualsIncome()
    {
        var entries = new[] { MakeIncome(1000m), MakeExpense(1000m) };

        var result = MonthlySummaryService.Compute(0m, entries);

        result.SavingsRatePct.Should().Be(0m);
        result.StatusColor.Should().Be(StatusColor.BloodRed);
    }

    [Fact]
    public void Compute_NegativeClosingBalance_WhenExpenseExceedsOpeningPlusIncome()
    {
        var entries = new[] { MakeExpense(500m) };

        var result = MonthlySummaryService.Compute(openingBalance: 100m, entries);

        result.ClosingBalance.Should().Be(-400m);
    }
}
