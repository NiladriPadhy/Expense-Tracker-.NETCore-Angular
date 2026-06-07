using ExpenseTracker.Application.Entries.Dtos;
using FluentValidation;

namespace ExpenseTracker.Application.Entries.Validators;

public sealed class EntryCreateValidator : AbstractValidator<EntryCreateDto>
{
    public EntryCreateValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x).Must(d => d.CategoryId.HasValue ^ !string.IsNullOrWhiteSpace(d.CategoryFreeText))
            .WithMessage("Provide exactly one of CategoryId or CategoryFreeText.");
    }
}

public sealed class EntryUpdateValidator : AbstractValidator<EntryUpdateDto>
{
    public EntryUpdateValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x).Must(d => d.CategoryId.HasValue ^ !string.IsNullOrWhiteSpace(d.CategoryFreeText))
            .WithMessage("Provide exactly one of CategoryId or CategoryFreeText.");
    }
}
