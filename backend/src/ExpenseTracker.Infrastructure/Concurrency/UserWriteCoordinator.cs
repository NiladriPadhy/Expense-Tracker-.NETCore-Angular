using System.Collections.Concurrent;
using ExpenseTracker.Domain.Abstractions;

namespace ExpenseTracker.Infrastructure.Concurrency;

/// <summary>
/// Serializes writes per user via per-UserId <see cref="SemaphoreSlim"/>. Singleton (R-004).
/// </summary>
public sealed class UserWriteCoordinator : IUserWriteCoordinator
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    private SemaphoreSlim Gate(Guid userId)
        => _gates.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

    public async Task<T> RunAsync<T>(Guid userId, Func<CancellationToken, Task<T>> work, CancellationToken ct)
    {
        var gate = Gate(userId);
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try { return await work(ct).ConfigureAwait(false); }
        finally { gate.Release(); }
    }

    public async Task RunAsync(Guid userId, Func<CancellationToken, Task> work, CancellationToken ct)
    {
        var gate = Gate(userId);
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try { await work(ct).ConfigureAwait(false); }
        finally { gate.Release(); }
    }
}
