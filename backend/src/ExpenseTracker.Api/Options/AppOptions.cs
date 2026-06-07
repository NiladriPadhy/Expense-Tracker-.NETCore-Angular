namespace ExpenseTracker.Api.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public string Provider { get; set; } = "Sqlite";
}

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";
    public int GlobalPerMinutePerIp { get; set; } = 100;
    public int AuthPerMinutePerIp { get; set; } = 5;
    public int AuthenticatedPerMinutePerUser { get; set; } = 60;
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "ExpenseTracker";
    public string Audience { get; set; } = "ExpenseTracker.Client";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}
