namespace ExpenseTracker.Domain.Entities;

public sealed class Currency
{
    private Currency() { }

    /// <summary>ISO 4217 alphabetic code (3 chars, uppercase). Primary key.</summary>
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Currency Create(string code, string name, string symbol, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
        {
            throw new ArgumentException("Currency code must be 3 characters.", nameof(code));
        }
        return new Currency
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name?.Trim() ?? string.Empty,
            Symbol = symbol?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void Update(string name, string symbol, DateTime nowUtc)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            Symbol = symbol.Trim();
        }
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTime nowUtc)
    {
        IsActive = true;
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }
}
