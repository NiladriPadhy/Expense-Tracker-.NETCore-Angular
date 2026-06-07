namespace ExpenseTracker.Application.Auth.Dtos;

public sealed record RegisterUserRequest(
    string FullName,
    string Email,
    string Phone,
    string Password,
    string CurrencyCode);

public sealed record LoginRequest(string Identifier, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserProfileDto User);

public sealed record UserProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string CurrencyCode,
    string Role,
    bool HasPhoto);
