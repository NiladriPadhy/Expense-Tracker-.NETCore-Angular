using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Entries.Dtos;

public sealed record EntryCreateDto(
    DateOnly EntryDate,
    EntryType Type,
    decimal Amount,
    Guid? CategoryId,
    string? CategoryFreeText,
    string? Note);

public sealed record EntryUpdateDto(
    DateOnly EntryDate,
    EntryType Type,
    decimal Amount,
    Guid? CategoryId,
    string? CategoryFreeText,
    string? Note);

public sealed record EntryDto(
    Guid Id,
    DateOnly EntryDate,
    EntryType Type,
    decimal Amount,
    string CurrencyCode,
    Guid? CategoryId,
    string CategoryName,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
