using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// Factory chooses the DB provider based on configuration <c>Database:Provider</c> (R-001).
/// Supported: Sqlite (default). SqlServer/PostgreSQL/MySQL can be wired via additional packages.
/// </summary>
public sealed class DbContextOptionsFactory
{
    private readonly string _provider;
    private readonly string _connectionString;

    public DbContextOptionsFactory(string provider, string connectionString)
    {
        _provider = string.IsNullOrWhiteSpace(provider) ? "Sqlite" : provider.Trim();
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public DbContextOptions<AppDbContext> Create()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        switch (_provider.ToLowerInvariant())
        {
            case "sqlite":
                builder.UseSqlite(_connectionString, sql =>
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
                break;
            // Additional providers may be configured here when their packages are added.
            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider '{_provider}'. Supported: Sqlite.");
        }
        return builder.Options;
    }

    public Action<DbContextOptionsBuilder> ConfigureAction => builder =>
    {
        switch (_provider.ToLowerInvariant())
        {
            case "sqlite":
                builder.UseSqlite(_connectionString, sql =>
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider '{_provider}'. Supported: Sqlite.");
        }
    };
}
