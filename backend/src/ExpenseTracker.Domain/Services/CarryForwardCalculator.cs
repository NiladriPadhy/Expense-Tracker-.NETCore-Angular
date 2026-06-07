using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.ValueObjects;

namespace ExpenseTracker.Domain.Services;

/// <summary>
/// Synchronously walks forward from the earliest affected month, recomputing MonthlySummary rows.
/// Per research R-004 / spec FR-019 + SC-003.
/// </summary>
public sealed class CarryForwardCalculator
{
    private readonly IEntryRepository _entries;
    private readonly IMonthlySummaryRepository _summaries;
    private readonly IClock _clock;

    public CarryForwardCalculator(IEntryRepository entries, IMonthlySummaryRepository summaries, IClock clock)
    {
        _entries = entries;
        _summaries = summaries;
        _clock = clock;
    }

    /// <summary>
    /// Recomputes the chain for <paramref name="userId"/> starting at <paramref name="earliestAffected"/>
    /// and walking to max(latest entry month, latest summary month).
    /// </summary>
    public async Task RecomputeAsync(
        Guid userId,
        string currencyCode,
        MonthYear earliestAffected,
        CancellationToken ct)
    {
        var latestEntry = await _entries.GetLatestEntryMonthAsync(userId, ct).ConfigureAwait(false);
        var latestSummary = await _summaries.GetLatestAsync(userId, ct).ConfigureAwait(false);

        var stop = earliestAffected;
        if (latestEntry is { } le && le > stop)
        {
            stop = le;
        }
        if (latestSummary is { } ls && ls.MonthYear > stop)
        {
            stop = ls.MonthYear;
        }

        // Opening balance for earliestAffected = closing balance of its previous month's summary (or 0).
        var prevSummary = await _summaries.GetAsync(userId, earliestAffected.Previous().Year, earliestAffected.Previous().Month, ct)
            .ConfigureAwait(false);
        var runningBalance = prevSummary?.ClosingBalance ?? 0m;

        var current = earliestAffected;
        while (true)
        {
            var monthEntries = await _entries.ListByMonthAsync(userId, current.Year, current.Month, ct).ConfigureAwait(false);
            var c = MonthlySummaryService.Compute(runningBalance, monthEntries);

            var existing = await _summaries.GetAsync(userId, current.Year, current.Month, ct).ConfigureAwait(false);
            if (existing is null)
            {
                var fresh = MonthlySummary.Create(
                    userId, current, c.OpeningBalance, c.TotalIncome, c.TotalExpense,
                    c.SavingsRatePct, c.StatusColor, currencyCode, _clock.UtcNow);
                await _summaries.AddAsync(fresh, ct).ConfigureAwait(false);
            }
            else
            {
                existing.Update(c.OpeningBalance, c.TotalIncome, c.TotalExpense, c.SavingsRatePct, c.StatusColor, _clock.UtcNow);
                _summaries.Update(existing);
            }

            runningBalance = c.ClosingBalance;
            if (current >= stop)
            {
                break;
            }
            current = current.Next();
        }
    }
}
