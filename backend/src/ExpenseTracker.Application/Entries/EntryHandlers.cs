using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Entries.Dtos;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Services;
using ExpenseTracker.Domain.ValueObjects;

namespace ExpenseTracker.Application.Entries;

internal static class EntryMapper
{
    public static EntryDto ToDto(Entry e, Category? cat)
    {
        var name = e.CategoryId is null
            ? (e.CategoryNameSnapshot ?? "Not listed")
            : (cat?.Name ?? e.CategoryNameSnapshot ?? "Not listed");
        return new EntryDto(
            e.Id, e.EntryDate, e.Type, e.Amount, e.CurrencyCode,
            e.CategoryId, name, e.Note, e.CreatedAtUtc, e.UpdatedAtUtc);
    }
}

public sealed class CreateEntryHandler
{
    private readonly IUserRepository _users;
    private readonly IEntryRepository _entries;
    private readonly ICategoryRepository _categories;
    private readonly IMonthlySummaryRepository _summaries;
    private readonly IUserWriteCoordinator _coord;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public CreateEntryHandler(
        IUserRepository users, IEntryRepository entries, ICategoryRepository categories,
        IMonthlySummaryRepository summaries, IUserWriteCoordinator coord, IUnitOfWork uow, IClock clock)
    {
        _users = users; _entries = entries; _categories = categories;
        _summaries = summaries; _coord = coord; _uow = uow; _clock = clock;
    }

    public Task<EntryDto> HandleAsync(Guid userId, EntryCreateDto dto, CancellationToken ct)
        => _coord.RunAsync(userId, async token =>
        {
            var user = await _users.GetByIdAsync(userId, token).ConfigureAwait(false)
                ?? throw new NotFoundException("user_not_found", "User not found.");

            Category? cat = null;
            EntryType? linkedType = null;
            if (dto.CategoryId is { } cid)
            {
                cat = await _categories.GetByIdAsync(cid, token).ConfigureAwait(false)
                    ?? throw new NotFoundException("category_not_found", "Category not found.");
                if (!cat.IsActive)
                {
                    throw new AppException("category_inactive", "Category is not active.");
                }
                linkedType = cat.Type;
            }

            var currentMonth = MonthYear.From(_clock.UtcNow);
            Entry entry;
            try
            {
                entry = Entry.Create(
                    user.Id, dto.EntryDate, dto.Type, dto.Amount, user.CurrencyCode,
                    dto.CategoryId, dto.CategoryFreeText, dto.Note,
                    linkedType, currentMonth, _clock.UtcNow);
            }
            catch (InvalidOperationException ex) when (ex.Message == "future_month_write_forbidden")
            {
                throw new AppException("future_month_write_forbidden", "Cannot write to a future month.");
            }

            await using var tx = await _uow.BeginTransactionAsync(token).ConfigureAwait(false);
            await _entries.AddAsync(entry, token).ConfigureAwait(false);
            await _uow.SaveChangesAsync(token).ConfigureAwait(false);

            var carry = new CarryForwardCalculator(_entries, _summaries, _clock);
            await carry.RecomputeAsync(user.Id, user.CurrencyCode, entry.MonthYear, token).ConfigureAwait(false);
            await _uow.SaveChangesAsync(token).ConfigureAwait(false);

            return EntryMapper.ToDto(entry, cat);
        }, ct);
}

public sealed class UpdateEntryHandler
{
    private readonly IUserRepository _users;
    private readonly IEntryRepository _entries;
    private readonly ICategoryRepository _categories;
    private readonly IMonthlySummaryRepository _summaries;
    private readonly IUserWriteCoordinator _coord;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public UpdateEntryHandler(
        IUserRepository users, IEntryRepository entries, ICategoryRepository categories,
        IMonthlySummaryRepository summaries, IUserWriteCoordinator coord, IUnitOfWork uow, IClock clock)
    {
        _users = users; _entries = entries; _categories = categories;
        _summaries = summaries; _coord = coord; _uow = uow; _clock = clock;
    }

    public Task<EntryDto> HandleAsync(Guid userId, Guid entryId, EntryUpdateDto dto, CancellationToken ct)
        => _coord.RunAsync(userId, async token =>
        {
            var entry = await _entries.GetByIdAsync(entryId, token).ConfigureAwait(false)
                ?? throw new NotFoundException("entry_not_found", "Entry not found.");
            if (entry.UserId != userId)
            {
                throw new ForbiddenException("entry_forbidden", "Not the entry owner.");
            }

            var user = await _users.GetByIdAsync(userId, token).ConfigureAwait(false)
                ?? throw new NotFoundException("user_not_found", "User not found.");

            Category? cat = null;
            EntryType? linkedType = null;
            if (dto.CategoryId is { } cid)
            {
                cat = await _categories.GetByIdAsync(cid, token).ConfigureAwait(false)
                    ?? throw new NotFoundException("category_not_found", "Category not found.");
                if (!cat.IsActive)
                {
                    throw new AppException("category_inactive", "Category is not active.");
                }
                linkedType = cat.Type;
            }

            var oldMonth = entry.MonthYear;
            var currentMonth = MonthYear.From(_clock.UtcNow);
            try
            {
                entry.Update(dto.EntryDate, dto.Type, dto.Amount, dto.CategoryId, dto.CategoryFreeText,
                    dto.Note, linkedType, currentMonth, _clock.UtcNow);
            }
            catch (InvalidOperationException ex) when (ex.Message == "future_month_write_forbidden")
            {
                throw new AppException("future_month_write_forbidden", "Cannot write to a future month.");
            }

            await using var tx = await _uow.BeginTransactionAsync(token).ConfigureAwait(false);
            _entries.Update(entry);
            await _uow.SaveChangesAsync(token).ConfigureAwait(false);

            var earliestAffected = oldMonth < entry.MonthYear ? oldMonth : entry.MonthYear;
            var carry = new CarryForwardCalculator(_entries, _summaries, _clock);
            await carry.RecomputeAsync(user.Id, user.CurrencyCode, earliestAffected, token).ConfigureAwait(false);
            await _uow.SaveChangesAsync(token).ConfigureAwait(false);

            return EntryMapper.ToDto(entry, cat);
        }, ct);
}

public sealed class DeleteEntryHandler
{
    private readonly IUserRepository _users;
    private readonly IEntryRepository _entries;
    private readonly IMonthlySummaryRepository _summaries;
    private readonly IUserWriteCoordinator _coord;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public DeleteEntryHandler(
        IUserRepository users, IEntryRepository entries, IMonthlySummaryRepository summaries,
        IUserWriteCoordinator coord, IUnitOfWork uow, IClock clock)
    {
        _users = users; _entries = entries; _summaries = summaries;
        _coord = coord; _uow = uow; _clock = clock;
    }

    public Task HandleAsync(Guid userId, Guid entryId, CancellationToken ct)
        => _coord.RunAsync(userId, async token =>
        {
            var entry = await _entries.GetByIdAsync(entryId, token).ConfigureAwait(false)
                ?? throw new NotFoundException("entry_not_found", "Entry not found.");
            if (entry.UserId != userId)
            {
                throw new ForbiddenException("entry_forbidden", "Not the entry owner.");
            }
            var user = await _users.GetByIdAsync(userId, token).ConfigureAwait(false)
                ?? throw new NotFoundException("user_not_found", "User not found.");

            var affectedMonth = entry.MonthYear;
            await using var tx = await _uow.BeginTransactionAsync(token).ConfigureAwait(false);
            _entries.Remove(entry);
            await _uow.SaveChangesAsync(token).ConfigureAwait(false);
            var carry = new CarryForwardCalculator(_entries, _summaries, _clock);
            await carry.RecomputeAsync(user.Id, user.CurrencyCode, affectedMonth, token).ConfigureAwait(false);
            await _uow.SaveChangesAsync(token).ConfigureAwait(false);
        }, ct);
}

public sealed class GetEntryHandler
{
    private readonly IEntryRepository _entries;
    private readonly ICategoryRepository _categories;

    public GetEntryHandler(IEntryRepository entries, ICategoryRepository categories)
    {
        _entries = entries; _categories = categories;
    }

    public async Task<EntryDto> HandleAsync(Guid userId, Guid entryId, CancellationToken ct)
    {
        var e = await _entries.GetByIdAsync(entryId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("entry_not_found", "Entry not found.");
        if (e.UserId != userId)
        {
            throw new ForbiddenException("entry_forbidden", "Not the entry owner.");
        }
        var cat = e.CategoryId is { } cid ? await _categories.GetByIdAsync(cid, ct).ConfigureAwait(false) : null;
        return EntryMapper.ToDto(e, cat);
    }
}

public sealed class ListEntriesByMonthHandler
{
    private readonly IEntryRepository _entries;
    private readonly ICategoryRepository _categories;

    public ListEntriesByMonthHandler(IEntryRepository entries, ICategoryRepository categories)
    {
        _entries = entries; _categories = categories;
    }

    public async Task<IReadOnlyList<EntryDto>> HandleAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var list = await _entries.ListByMonthAsync(userId, year, month, ct).ConfigureAwait(false);
        var categoryIds = list.Where(e => e.CategoryId.HasValue).Select(e => e.CategoryId!.Value).Distinct().ToList();
        var allCats = await _categories.ListAllAsync(ct).ConfigureAwait(false);
        var catMap = allCats.Where(c => categoryIds.Contains(c.Id)).ToDictionary(c => c.Id);
        return list.Select(e => EntryMapper.ToDto(e, e.CategoryId is { } cid && catMap.TryGetValue(cid, out var c) ? c : null)).ToList();
    }
}
