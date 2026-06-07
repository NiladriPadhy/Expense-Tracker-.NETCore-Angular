using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.Infrastructure.Identity;

public sealed class JwtOptionsValues
{
    public string Issuer { get; init; } = "ExpenseTracker";
    public string Audience { get; init; } = "ExpenseTracker.Client";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 60;
    public int RefreshTokenDays { get; init; } = 30;
}

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptionsValues _opts;
    private readonly IClock _clock;

    public JwtTokenService(JwtOptionsValues opts, IClock clock)
    {
        _opts = opts;
        _clock = clock;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) IssueAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_opts.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim("currency", user.CurrencyCode),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string PlainRefreshToken, string TokenHash, DateTime ExpiresAtUtc) IssueRefreshToken(User user)
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        var plain = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var expires = _clock.UtcNow.AddDays(_opts.RefreshTokenDays);
        return (plain, HashRefreshToken(plain), expires);
    }

    public string HashRefreshToken(string plain)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(hash);
    }
}
