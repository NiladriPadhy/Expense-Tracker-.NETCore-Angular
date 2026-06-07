using ExpenseTracker.Application.Entries.Dtos;
using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Months.Dtos;

public sealed record MonthlyViewDto(
    int Year,
    int Month,
    string CurrencyCode,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal ClosingBalance,
    decimal SavingsRatePct,
    StatusColor StatusColor,
    bool ReadOnly,
    IReadOnlyList<EntryDto> Entries);
