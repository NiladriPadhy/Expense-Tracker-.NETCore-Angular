using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using ExpenseTracker.Api.Authorization;
using ExpenseTracker.Api.Middleware;
using ExpenseTracker.Api.Options;
using ExpenseTracker.Application.Admin.Categories;
using ExpenseTracker.Application.Admin.Currencies;
using ExpenseTracker.Application.Admin.Users;
using ExpenseTracker.Application.Auth;
using ExpenseTracker.Application.Auth.Validators;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Dashboard;
using ExpenseTracker.Application.Entries;
using ExpenseTracker.Application.Entries.Validators;
using ExpenseTracker.Application.Months;
using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Infrastructure.Concurrency;
using ExpenseTracker.Infrastructure.Identity;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Persistence.Repositories;
using ExpenseTracker.Infrastructure.Persistence.Seeding;
using ExpenseTracker.Infrastructure.Storage;
using ExpenseTracker.Infrastructure.Time;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ExpenseTracker.Api")
    .WriteTo.Console());

// Options
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// DbContext via factory (R-001)
var dbProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connStr = builder.Configuration.GetConnectionString("Default")
              ?? "Data Source=expensetracker.db";
var optionsFactory = new DbContextOptionsFactory(dbProvider, connStr);
builder.Services.AddSingleton(optionsFactory);
builder.Services.AddDbContext<AppDbContext>(optionsFactory.ConfigureAction);

// Domain & Infra services
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IUserWriteCoordinator, UserWriteCoordinator>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var signingKey = jwtSection["SigningKey"] ?? "dev-only-signing-key-please-change-32bytes!!";
if (!builder.Environment.IsDevelopment() && signingKey.Contains("dev-only", StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is set to the development default in a non-Development environment. " +
        "Override it via environment variable (Jwt__SigningKey) or user-secrets before deploying.");
}
var jwtOptValues = new JwtOptionsValues
{
    Issuer = jwtSection["Issuer"] ?? "ExpenseTracker",
    Audience = jwtSection["Audience"] ?? "ExpenseTracker.Client",
    SigningKey = signingKey,
    AccessTokenMinutes = int.TryParse(jwtSection["AccessTokenMinutes"], out var am) ? am : 60,
    RefreshTokenDays = int.TryParse(jwtSection["RefreshTokenDays"], out var rd) ? rd : 30,
};
builder.Services.AddSingleton(jwtOptValues);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IEntryRepository, EntryRepository>();
builder.Services.AddScoped<IMonthlySummaryRepository, MonthlySummaryRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPhotoStorage, DatabasePhotoStorage>();
builder.Services.AddScoped<Seeder>();

// Handlers
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginUserHandler>();
builder.Services.AddScoped<RefreshTokenHandler>();
builder.Services.AddScoped<LogoutHandler>();
builder.Services.AddScoped<CreateEntryHandler>();
builder.Services.AddScoped<UpdateEntryHandler>();
builder.Services.AddScoped<DeleteEntryHandler>();
builder.Services.AddScoped<GetEntryHandler>();
builder.Services.AddScoped<ListEntriesByMonthHandler>();
builder.Services.AddScoped<GetMonthlyViewHandler>();
builder.Services.AddScoped<GetDashboardHandler>();
builder.Services.AddScoped<ListUsersHandler>();
builder.Services.AddScoped<GetUserHandler>();
builder.Services.AddScoped<UpdateUserHandler>();
builder.Services.AddScoped<DeleteUserHandler>();
builder.Services.AddScoped<CreateCategoryHandler>();
builder.Services.AddScoped<UpdateCategoryHandler>();
builder.Services.AddScoped<DeactivateCategoryHandler>();
builder.Services.AddScoped<CreateCurrencyHandler>();
builder.Services.AddScoped<UpdateCurrencyHandler>();
builder.Services.AddScoped<DeactivateCurrencyHandler>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<EntryCreateValidator>();
builder.Services.AddFluentValidationAutoValidation();

// AuthN/Z
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.MapInboundClaims = false;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptValues.Issuer,
            ValidAudience = jwtOptValues.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role",
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy(PolicyNames.RequireUser, p => p.RequireAuthenticatedUser());
    opts.AddPolicy(PolicyNames.RequireAdmin, p => p.RequireAuthenticatedUser().RequireClaim("role", "Admin"));
});
builder.Services.AddSingleton<IAuthorizationHandler, EntryOwnerAuthorizationHandler>();

// CORS
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (origins.Length == 0)
        {
            p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
        else
        {
            p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

// Rate limiter (R-009)
var rateOpts = builder.Configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = 429;
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateOpts.GlobalPerMinutePerIp,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
    opts.AddPolicy("auth", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = rateOpts.AuthPerMinutePerIp,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = 0,
        });
    });
    opts.AddPolicy("user", httpContext =>
    {
        var uid = httpContext.User.GetUserId();
        var key = uid == Guid.Empty ? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon" : uid.ToString();
        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = rateOpts.AuthenticatedPerMinutePerUser,
            TokensPerPeriod = rateOpts.AuthenticatedPerMinutePerUser,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    });
});

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ExpenseTracker API", Version = "v1" });
    c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "bearerAuth", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>(),
    });
});

var app = builder.Build();

// Pipeline (order matters)
app.UseMiddleware<ExceptionHandlerMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

// Seed on startup (skip when launched from WebApplicationFactory tests via env flag)
if (Environment.GetEnvironmentVariable("EXPENSETRACKER_SKIP_SEED") != "true")
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<Seeder>();
    await seeder.RunAsync();
}

app.Run();

public partial class Program { }
