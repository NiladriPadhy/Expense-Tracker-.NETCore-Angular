using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Dashboard.Dtos;

public sealed record DashboardMonthPointDto(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Savings,
    decimal SavingsRatePct,
    StatusColor StatusColor);

public sealed record DashboardDto(
    string CurrencyCode,
    decimal CurrentMonthSavingsRatePct,
    StatusColor CurrentMonthStatusColor,
    bool AlertExpenseExceedsIncome,
    IReadOnlyList<DashboardMonthPointDto> Trend);
