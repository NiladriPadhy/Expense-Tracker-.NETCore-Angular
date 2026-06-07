using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.ValueObjects;

namespace ExpenseTracker.Domain.Services;

/// <summary>
/// Pure computation: given entries for a month + opening balance, computes totals/savings/status.
/// </summary>
public static class MonthlySummaryService
{
    public readonly record struct Computation(
        decimal OpeningBalance,
        decimal TotalIncome,
        decimal TotalExpense,
        decimal ClosingBalance,
        decimal SavingsRatePct,
        StatusColor StatusColor);

    public static Computation Compute(decimal openingBalance, IEnumerable<Entry> entries)
    {
        decimal income = 0m, expense = 0m;
        foreach (var e in entries)
        {
            if (e.Type == EntryType.Income)
            {
                income += e.Amount;
            }
            else
            {
                expense += e.Amount;
            }
        }

        var rate = SavingsRateClassifier.ComputeRate(income, expense);
        var color = SavingsRateClassifier.Classify(income, expense);
        return new Computation(openingBalance, income, expense, openingBalance + income - expense, rate, color);
    }
}
