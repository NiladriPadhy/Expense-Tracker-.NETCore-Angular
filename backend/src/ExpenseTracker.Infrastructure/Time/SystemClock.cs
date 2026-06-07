using ExpenseTracker.Domain.Abstractions;

namespace ExpenseTracker.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
