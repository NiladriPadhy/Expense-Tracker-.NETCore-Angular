using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities;

public sealed class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string CurrencyCode { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.User;
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public UserProfilePhoto? Photo { get; private set; }
    public Guid? PhotoId { get; private set; }

    public static User Create(
        string fullName,
        string email,
        string phone,
        string passwordHash,
        string currencyCode,
        DateTime nowUtc,
        UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name required.", nameof(fullName));
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email required.", nameof(email));
        }
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Phone required.", nameof(phone));
        }
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash required.", nameof(passwordHash));
        }
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency required.", nameof(currencyCode));
        }

        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone.Trim(),
            PasswordHash = passwordHash,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Role = role,
            IsActive = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void UpdateProfile(string fullName, string phone, string currencyCode, DateTime nowUtc)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            FullName = fullName.Trim();
        }
        if (!string.IsNullOrWhiteSpace(phone))
        {
            Phone = phone.Trim();
        }
        if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        }
        UpdatedAtUtc = nowUtc;
    }

    public void ChangeRole(UserRole newRole, DateTime nowUtc)
    {
        Role = newRole;
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

    public void SoftDelete(DateTime nowUtc)
    {
        IsDeleted = true;
        IsActive = false;
        DeletedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void SetPasswordHash(string newHash, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(newHash))
        {
            throw new ArgumentException("Password hash required.", nameof(newHash));
        }
        PasswordHash = newHash;
        UpdatedAtUtc = nowUtc;
    }

    public void AttachPhoto(UserProfilePhoto photo, DateTime nowUtc)
    {
        Photo = photo;
        PhotoId = photo.Id;
        UpdatedAtUtc = nowUtc;
    }

    public void RemovePhoto(DateTime nowUtc)
    {
        Photo = null;
        PhotoId = null;
        UpdatedAtUtc = nowUtc;
    }
}
