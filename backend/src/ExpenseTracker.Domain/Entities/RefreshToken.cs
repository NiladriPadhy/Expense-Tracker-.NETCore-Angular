namespace ExpenseTracker.Domain.Entities;

public sealed class RefreshToken
{
    private RefreshToken() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TokenHash = tokenHash,
        ExpiresAtUtc = expiresAtUtc,
        CreatedAtUtc = nowUtc,
    };

    public void Revoke(DateTime nowUtc, Guid? replacedBy = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }
        RevokedAtUtc = nowUtc;
        ReplacedByTokenId = replacedBy;
    }
}
