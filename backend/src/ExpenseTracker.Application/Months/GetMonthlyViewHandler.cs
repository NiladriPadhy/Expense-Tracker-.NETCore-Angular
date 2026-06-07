using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Entries;
using ExpenseTracker.Application.Months.Dtos;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Services;
using ExpenseTracker.Domain.ValueObjects;

namespace ExpenseTracker.Application.Months;

public sealed class GetMonthlyViewHandler
{
    private readonly IUserRepository _users;
    private readonly IEntryRepository _entries;
    private readonly ICategoryRepository _categories;
    private readonly IMonthlySummaryRepository _summaries;
    private readonly IClock _clock;

    public GetMonthlyViewHandler(
        IUserRepository users, IEntryRepository entries, ICategoryRepository categories,
        IMonthlySummaryRepository summaries, IClock clock)
    {
        _users = users; _entries = entries; _categories = categories;
        _summaries = summaries; _clock = clock;
    }

    public async Task<MonthlyViewDto> HandleAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("user_not_found", "User not found.");
        var target = new MonthYear(year, month);
        var currentMonth = MonthYear.From(_clock.UtcNow);

        if (target > currentMonth)
        {
            // Projection: walk closing balances forward from latest summary to target's previous month.
            var latest = await _summaries.GetLatestAsync(userId, ct).ConfigureAwait(false);
            var openingProjection = latest?.ClosingBalance ?? 0m;
            // If gap exists (no summaries between latest and target-1), opening balance carries unchanged.
            return new MonthlyViewDto(
                year, month, user.CurrencyCode,
                openingProjection, 0m, 0m, openingProjection,
                0m, StatusColor.BloodRed,
                ReadOnly: true,
                Entries: Array.Empty<Entries.Dtos.EntryDto>());
        }

        var entries = await _entries.ListByMonthAsync(userId, year, month, ct).ConfigureAwait(false);
        var summary = await _summaries.GetAsync(userId, year, month, ct).ConfigureAwait(false);

        decimal opening, income, expense, closing, rate;
        StatusColor color;
        if (summary is not null)
        {
            opening = summary.OpeningBalance;
            income = summary.TotalIncome;
            expense = summary.TotalExpense;
            closing = summary.ClosingBalance;
            rate = summary.SavingsRatePct;
            color = summary.StatusColor;
        }
        else
        {
            var prev = await _summaries.GetAsync(userId, target.Previous().Year, target.Previous().Month, ct).ConfigureAwait(false);
            var c = MonthlySummaryService.Compute(prev?.ClosingBalance ?? 0m, entries);
            opening = c.OpeningBalance;
            income = c.TotalIncome;
            expense = c.TotalExpense;
            closing = c.ClosingBalance;
            rate = c.SavingsRatePct;
            color = c.StatusColor;
        }

        var allCats = await _categories.ListAllAsync(ct).ConfigureAwait(false);
        var catMap = allCats.ToDictionary(c => c.Id);
        var dtos = entries.Select(e => EntryMapper.ToDto(
            e, e.CategoryId is { } cid && catMap.TryGetValue(cid, out var c) ? c : null)).ToList();

        return new MonthlyViewDto(
            year, month, user.CurrencyCode,
            opening, income, expense, closing, rate, color,
            ReadOnly: false,
            Entries: dtos);
    }
}
