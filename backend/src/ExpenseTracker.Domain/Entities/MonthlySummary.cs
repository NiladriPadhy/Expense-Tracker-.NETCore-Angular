using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.ValueObjects;

namespace ExpenseTracker.Domain.Entities;

public sealed class MonthlySummary
{
    private MonthlySummary() { }

    public Guid UserId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal TotalIncome { get; private set; }
    public decimal TotalExpense { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public decimal SavingsRatePct { get; private set; }
    public StatusColor StatusColor { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; private set; }

    public MonthYear MonthYear => new(Year, Month);

    public static MonthlySummary Create(
        Guid userId,
        MonthYear monthYear,
        decimal openingBalance,
        decimal totalIncome,
        decimal totalExpense,
        decimal savingsRatePct,
        StatusColor color,
        string currencyCode,
        DateTime nowUtc) => new()
        {
            UserId = userId,
            Year = monthYear.Year,
            Month = monthYear.Month,
            OpeningBalance = openingBalance,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            ClosingBalance = openingBalance + totalIncome - totalExpense,
            SavingsRatePct = savingsRatePct,
            StatusColor = color,
            CurrencyCode = currencyCode,
            UpdatedAtUtc = nowUtc,
        };

    public void Update(
        decimal openingBalance,
        decimal totalIncome,
        decimal totalExpense,
        decimal savingsRatePct,
        StatusColor color,
        DateTime nowUtc)
    {
        OpeningBalance = openingBalance;
        TotalIncome = totalIncome;
        TotalExpense = totalExpense;
        ClosingBalance = openingBalance + totalIncome - totalExpense;
        SavingsRatePct = savingsRatePct;
        StatusColor = color;
        UpdatedAtUtc = nowUtc;
    }
}
