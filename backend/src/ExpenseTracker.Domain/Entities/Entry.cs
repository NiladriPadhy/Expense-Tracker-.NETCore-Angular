using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.ValueObjects;

namespace ExpenseTracker.Domain.Entities;

public sealed class Entry
{
    private Entry() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public EntryType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;

    /// <summary>FK to Category. Null when user picked "Not listed" and provided a free-text label.</summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// Snapshot of category name AT WRITE TIME.
    /// Used ONLY when CategoryId is NULL (free-text). When CategoryId is set, the live Category.Name is authoritative
    /// (rename of a linked category propagates per US7 ACS#2 live-join semantics).
    /// </summary>
    public string? CategoryNameSnapshot { get; private set; }

    public string? Note { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public MonthYear MonthYear => new(EntryDate.Year, EntryDate.Month);

    public static Entry Create(
        Guid userId,
        DateOnly entryDate,
        EntryType type,
        decimal amount,
        string currencyCode,
        Guid? categoryId,
        string? categoryFreeText,
        string? note,
        EntryType? linkedCategoryType,
        MonthYear currentMonthForUser,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId required.", nameof(userId));
        }
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0.");
        }
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("CurrencyCode required.", nameof(currencyCode));
        }

        EnsureCategorySelection(type, categoryId, categoryFreeText, linkedCategoryType);
        EnsureNotFutureMonth(entryDate, currentMonthForUser);

        return new Entry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntryDate = entryDate,
            Type = type,
            Amount = decimal.Round(amount, 2, MidpointRounding.ToEven),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            CategoryId = categoryId,
            CategoryNameSnapshot = categoryId is null
                ? categoryFreeText!.Trim()
                : null,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void Update(
        DateOnly entryDate,
        EntryType type,
        decimal amount,
        Guid? categoryId,
        string? categoryFreeText,
        string? note,
        EntryType? linkedCategoryType,
        MonthYear currentMonthForUser,
        DateTime nowUtc)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0.");
        }

        EnsureCategorySelection(type, categoryId, categoryFreeText, linkedCategoryType);
        EnsureNotFutureMonth(entryDate, currentMonthForUser);

        EntryDate = entryDate;
        Type = type;
        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        CategoryId = categoryId;
        CategoryNameSnapshot = categoryId is null ? categoryFreeText!.Trim() : null;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UpdatedAtUtc = nowUtc;
    }

    private static void EnsureCategorySelection(
        EntryType type,
        Guid? categoryId,
        string? freeText,
        EntryType? linkedCategoryType)
    {
        var hasCategory = categoryId.HasValue;
        var hasFreeText = !string.IsNullOrWhiteSpace(freeText);

        if (hasCategory == hasFreeText)
        {
            throw new ArgumentException(
                "Provide either a CategoryId or a free-text category label, but not both and not neither.");
        }

        if (hasCategory && linkedCategoryType is not null && linkedCategoryType != type)
        {
            throw new ArgumentException(
                "Linked category type does not match entry type (Expense vs Income).");
        }
    }

    private static void EnsureNotFutureMonth(DateOnly entryDate, MonthYear currentMonth)
    {
        var entryMonth = new MonthYear(entryDate.Year, entryDate.Month);
        if (entryMonth > currentMonth)
        {
            throw new InvalidOperationException(
                "future_month_write_forbidden");
        }
    }
}
