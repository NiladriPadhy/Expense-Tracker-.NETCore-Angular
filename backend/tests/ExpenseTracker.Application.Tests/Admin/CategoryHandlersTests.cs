using ExpenseTracker.Application.Admin.Categories;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Tests.Auth;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Application.Tests.Admin;

public class CategoryHandlersTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_NewCategory_ReturnsDto()
    {
        var fakes = new HandlerFakes();
        var repo = new FakeCategoryRepo();
        var handler = new CreateCategoryHandler(repo, fakes.Uow, fakes.Clock);

        var dto = await handler.HandleAsync(new CreateCategoryDto("Groceries", EntryType.Expense), CancellationToken.None);

        dto.Name.Should().Be("Groceries");
        dto.Type.Should().Be(EntryType.Expense);
        dto.IsActive.Should().BeTrue();
        repo.Added.Should().HaveCount(1);
        fakes.Uow.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_DuplicateName_Throws()
    {
        var fakes = new HandlerFakes();
        var repo = new FakeCategoryRepo { ExistingNames = { ("groceries", EntryType.Expense) } };
        var handler = new CreateCategoryHandler(repo, fakes.Uow, fakes.Clock);

        await handler.Invoking(h => h.HandleAsync(new CreateCategoryDto("Groceries", EntryType.Expense), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>().Where(e => e.Code == "category_name_exists");
    }

    [Fact]
    public async Task Update_RenamesAndDeactivates()
    {
        var fakes = new HandlerFakes();
        var repo = new FakeCategoryRepo();
        var existing = Category.Create("Food", EntryType.Expense, Now);
        repo.Store[existing.Id] = existing;

        var handler = new UpdateCategoryHandler(repo, fakes.Uow, fakes.Clock);
        var result = await handler.HandleAsync(existing.Id, new UpdateCategoryDto("Dining", false), CancellationToken.None);

        result.Name.Should().Be("Dining");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Update_NotFound_Throws()
    {
        var fakes = new HandlerFakes();
        var repo = new FakeCategoryRepo();
        var handler = new UpdateCategoryHandler(repo, fakes.Uow, fakes.Clock);

        await handler.Invoking(h => h.HandleAsync(Guid.NewGuid(), new UpdateCategoryDto("X", null), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_RenameToConflictingName_Throws()
    {
        var fakes = new HandlerFakes();
        var repo = new FakeCategoryRepo { ExistingNames = { ("dining", EntryType.Expense) } };
        var existing = Category.Create("Food", EntryType.Expense, Now);
        repo.Store[existing.Id] = existing;

        var handler = new UpdateCategoryHandler(repo, fakes.Uow, fakes.Clock);

        await handler.Invoking(h => h.HandleAsync(existing.Id, new UpdateCategoryDto("Dining", null), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_Activate_ReactivatesCategory()
    {
        var fakes = new HandlerFakes();
        var repo = new FakeCategoryRepo();
        var existing = Category.Create("Food", EntryType.Expense, Now);
        existing.Deactivate(Now);
        repo.Store[existing.Id] = existing;

        var handler = new UpdateCategoryHandler(repo, fakes.Uow, fakes.Clock);
        var result = await handler.HandleAsync(existing.Id, new UpdateCategoryDto(null, true), CancellationToken.None);

        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_FoundCategory_Deactivates()
    {
        var fakes = new HandlerFakes();
        var repo = new FakeCategoryRepo();
        var existing = Category.Create("Food", EntryType.Expense, Now);
        repo.Store[existing.Id] = existing;

        var handler = new DeactivateCategoryHandler(repo, fakes.Uow, fakes.Clock);
        await handler.HandleAsync(existing.Id, CancellationToken.None);

        existing.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_NotFound_Throws()
    {
        var fakes = new HandlerFakes();
        var repo = new FakeCategoryRepo();
        var handler = new DeactivateCategoryHandler(repo, fakes.Uow, fakes.Clock);

        await handler.Invoking(h => h.HandleAsync(Guid.NewGuid(), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}

internal sealed class FakeCategoryRepo : ICategoryRepository
{
    public Dictionary<Guid, Category> Store { get; } = new();
    public List<Category> Added { get; } = new();
    public HashSet<(string Name, EntryType Type)> ExistingNames { get; } = new();

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(Store.TryGetValue(id, out var c) ? c : null);

    public Task<IReadOnlyList<Category>> ListActiveAsync(EntryType? type, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Category>>(Store.Values.Where(c => c.IsActive && (type is null || c.Type == type)).ToList());

    public Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Category>>(Store.Values.ToList());

    public Task<bool> NameExistsAsync(string name, EntryType type, Guid? excludingId, CancellationToken ct)
        => Task.FromResult(ExistingNames.Contains((name.Trim().ToLowerInvariant(), type)));

    public Task AddAsync(Category category, CancellationToken ct)
    {
        Store[category.Id] = category;
        Added.Add(category);
        return Task.CompletedTask;
    }

    public void Update(Category category) { Store[category.Id] = category; }
}
