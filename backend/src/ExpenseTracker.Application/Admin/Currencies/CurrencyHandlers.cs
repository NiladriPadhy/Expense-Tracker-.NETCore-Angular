using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Application.Admin.Currencies;

public sealed record AdminCurrencyDto(string Code, string Name, string Symbol, bool IsActive);
public sealed record CreateCurrencyDto(string Code, string Name, string Symbol);
public sealed record UpdateCurrencyDto(string? Name, string? Symbol, bool? IsActive);

public sealed class CreateCurrencyHandler
{
    private readonly ICurrencyRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    public CreateCurrencyHandler(ICurrencyRepository repo, IUnitOfWork uow, IClock clock)
    { _repo = repo; _uow = uow; _clock = clock; }

    public async Task<AdminCurrencyDto> HandleAsync(CreateCurrencyDto dto, CancellationToken ct)
    {
        var existing = await _repo.GetByCodeAsync(dto.Code, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            throw new ConflictException("currency_exists", "Currency already exists.");
        }
        var c = Currency.Create(dto.Code, dto.Name, dto.Symbol, _clock.UtcNow);
        await _repo.AddAsync(c, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return new AdminCurrencyDto(c.Code, c.Name, c.Symbol, c.IsActive);
    }
}

public sealed class UpdateCurrencyHandler
{
    private readonly ICurrencyRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    public UpdateCurrencyHandler(ICurrencyRepository repo, IUnitOfWork uow, IClock clock)
    { _repo = repo; _uow = uow; _clock = clock; }

    public async Task<AdminCurrencyDto> HandleAsync(string code, UpdateCurrencyDto dto, CancellationToken ct)
    {
        var c = await _repo.GetByCodeAsync(code, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("currency_not_found", "Currency not found.");
        c.Update(dto.Name ?? c.Name, dto.Symbol ?? c.Symbol, _clock.UtcNow);
        if (dto.IsActive is { } active)
        {
            if (active) c.Activate(_clock.UtcNow); else c.Deactivate(_clock.UtcNow);
        }
        _repo.Update(c);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return new AdminCurrencyDto(c.Code, c.Name, c.Symbol, c.IsActive);
    }
}

public sealed class DeactivateCurrencyHandler
{
    private readonly ICurrencyRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    public DeactivateCurrencyHandler(ICurrencyRepository repo, IUnitOfWork uow, IClock clock)
    { _repo = repo; _uow = uow; _clock = clock; }

    public async Task HandleAsync(string code, bool hard, CancellationToken ct)
    {
        var c = await _repo.GetByCodeAsync(code, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("currency_not_found", "Currency not found.");
        if (hard)
        {
            if (await _repo.IsReferencedByAnyUserAsync(c.Code, ct).ConfigureAwait(false))
            {
                throw new ConflictException("currency_in_use", "Currency referenced by users; cannot hard-delete.");
            }
            _repo.Remove(c);
        }
        else
        {
            c.Deactivate(_clock.UtcNow);
            _repo.Update(c);
        }
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
