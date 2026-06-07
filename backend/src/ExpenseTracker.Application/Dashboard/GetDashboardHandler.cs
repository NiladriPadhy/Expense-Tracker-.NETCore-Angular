using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Dashboard.Dtos;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Services;
using ExpenseTracker.Domain.ValueObjects;

namespace ExpenseTracker.Application.Dashboard;

public sealed class GetDashboardHandler
{
    private readonly IUserRepository _users;
    private readonly IMonthlySummaryRepository _summaries;
    private readonly IEntryRepository _entries;
    private readonly IClock _clock;

    public GetDashboardHandler(
        IUserRepository users,
        IMonthlySummaryRepository summaries,
        IEntryRepository entries,
        IClock clock)
    {
        _users = users; _summaries = summaries; _entries = entries; _clock = clock;
    }

    public async Task<DashboardDto> HandleAsync(Guid userId, int monthsBack, CancellationToken ct)
    {
        if (monthsBack < 1) monthsBack = 6;
        if (monthsBack > 36) monthsBack = 36;

        var user = await _users.GetByIdAsync(userId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("user_not_found", "User not found.");

        var now = MonthYear.From(_clock.UtcNow);
        var summaries = await _summaries.ListLastAsync(userId, monthsBack, now.Year, now.Month, ct).ConfigureAwait(false);

        var trend = new List<DashboardMonthPointDto>(summaries.Count);
        DashboardMonthPointDto? currentPoint = null;
        foreach (var s in summaries)
        {
            var p = new DashboardMonthPointDto(
                s.Year, s.Month, s.TotalIncome, s.TotalExpense,
                s.TotalIncome - s.TotalExpense, s.SavingsRatePct, s.StatusColor);
            trend.Add(p);
            if (s.Year == now.Year && s.Month == now.Month) currentPoint = p;
        }

        // If no summary for current month yet, compute on the fly from entries.
        decimal currentRate;
        StatusColor currentColor;
        bool alertExceed;
        if (currentPoint is not null)
        {
            currentRate = currentPoint.SavingsRatePct;
            currentColor = currentPoint.StatusColor;
            alertExceed = currentPoint.TotalExpense >= currentPoint.TotalIncome;
        }
        else
        {
            var monthEntries = await _entries.ListByMonthAsync(userId, now.Year, now.Month, ct).ConfigureAwait(false);
            var c = MonthlySummaryService.Compute(0m, monthEntries);
            currentRate = c.SavingsRatePct;
            currentColor = c.StatusColor;
            alertExceed = c.TotalExpense >= c.TotalIncome && (c.TotalIncome > 0m || c.TotalExpense > 0m);
        }

        return new DashboardDto(
            user.CurrencyCode, currentRate, currentColor, alertExceed, trend);
    }
}
