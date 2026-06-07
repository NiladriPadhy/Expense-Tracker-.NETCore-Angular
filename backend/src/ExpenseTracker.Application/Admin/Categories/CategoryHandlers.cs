using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Admin.Categories;

public sealed record AdminCategoryDto(Guid Id, string Name, EntryType Type, bool IsActive);
public sealed record CreateCategoryDto(string Name, EntryType Type);
public sealed record UpdateCategoryDto(string? Name, bool? IsActive);

public sealed class CreateCategoryHandler
{
    private readonly ICategoryRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    public CreateCategoryHandler(ICategoryRepository repo, IUnitOfWork uow, IClock clock)
    { _repo = repo; _uow = uow; _clock = clock; }

    public async Task<AdminCategoryDto> HandleAsync(CreateCategoryDto dto, CancellationToken ct)
    {
        if (await _repo.NameExistsAsync(dto.Name, dto.Type, null, ct).ConfigureAwait(false))
        {
            throw new ConflictException("category_name_exists", "Category with this name already exists for this type.");
        }
        var c = Category.Create(dto.Name, dto.Type, _clock.UtcNow);
        await _repo.AddAsync(c, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return new AdminCategoryDto(c.Id, c.Name, c.Type, c.IsActive);
    }
}

public sealed class UpdateCategoryHandler
{
    private readonly ICategoryRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    public UpdateCategoryHandler(ICategoryRepository repo, IUnitOfWork uow, IClock clock)
    { _repo = repo; _uow = uow; _clock = clock; }

    public async Task<AdminCategoryDto> HandleAsync(Guid id, UpdateCategoryDto dto, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("category_not_found", "Category not found.");
        if (!string.IsNullOrWhiteSpace(dto.Name) && !string.Equals(dto.Name.Trim(), c.Name, StringComparison.Ordinal))
        {
            if (await _repo.NameExistsAsync(dto.Name, c.Type, c.Id, ct).ConfigureAwait(false))
            {
                throw new ConflictException("category_name_exists", "Category with this name already exists for this type.");
            }
            c.Rename(dto.Name, _clock.UtcNow);
        }
        if (dto.IsActive is { } active)
        {
            if (active) c.Activate(_clock.UtcNow); else c.Deactivate(_clock.UtcNow);
        }
        _repo.Update(c);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return new AdminCategoryDto(c.Id, c.Name, c.Type, c.IsActive);
    }
}

public sealed class DeactivateCategoryHandler
{
    private readonly ICategoryRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    public DeactivateCategoryHandler(ICategoryRepository repo, IUnitOfWork uow, IClock clock)
    { _repo = repo; _uow = uow; _clock = clock; }

    public async Task HandleAsync(Guid id, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("category_not_found", "Category not found.");
        c.Deactivate(_clock.UtcNow);
        _repo.Update(c);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
