using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.Api.Options;
using ExpenseTracker.Application.Auth.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExpenseTracker.Api.IntegrationTests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"itest-{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set BEFORE host build so Program.cs's `builder.Configuration.Get<RateLimitOptions>()`
        // reads our overrides (it captures into a local at composition time).
        Environment.SetEnvironmentVariable("EXPENSETRACKER_SKIP_SEED", "false");
        Environment.SetEnvironmentVariable("RateLimit__GlobalPerMinutePerIp", "100000");
        Environment.SetEnvironmentVariable("RateLimit__AuthPerMinutePerIp", "100000");
        Environment.SetEnvironmentVariable("RateLimit__AuthenticatedPerMinutePerUser", "100000");

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbName}",
                ["Database:Provider"] = "Sqlite",
                ["Seed:DefaultAdminEmail"] = "admin@test.local",
                ["Seed:DefaultAdminPassword"] = "AdminPwd123!",
                ["Seed:DefaultAdminPhone"] = "+10000000001",
                ["Seed:DefaultAdminCurrency"] = "USD",
                ["Jwt:SigningKey"] = "test-only-signing-key-32-bytes-min!!",
                ["RateLimit:GlobalPerMinutePerIp"] = "100000",
                ["RateLimit:AuthPerMinutePerIp"] = "100000",
                ["RateLimit:AuthenticatedPerMinutePerUser"] = "100000",
            });
        });
        builder.ConfigureServices(services =>
        {
            // Belt-and-braces: ensure RateLimitOptions reflects huge limits
            // even if the rate limiter was built with cached defaults.
            services.PostConfigure<RateLimitOptions>(o =>
            {
                o.GlobalPerMinutePerIp = 100000;
                o.AuthPerMinutePerIp = 100000;
                o.AuthenticatedPerMinutePerUser = 100000;
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbName); } catch { /* ignore */ }
    }
}

public class AuthEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    public AuthEndpointsTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_Anonymous_Returns200()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActiveCurrencies_Anonymous_ReturnsList()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/currencies/active");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        list.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task RegisterJson_Then_Login_Succeeds()
    {
        var client = _factory.CreateClient();
        var register = new RegisterUserRequest(
            "Test User", $"u{Guid.NewGuid():N}@test.local", $"+1415555{Random.Shared.Next(1000, 9999)}",
            "Password123!", "USD");
        var r1 = await client.PostAsJsonAsync("/api/v1/auth/register-json", register);
        r1.EnsureSuccessStatusCode();
        var auth = await r1.Content.ReadFromJsonAsync<AuthResult>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();

        var r2 = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(register.Email, register.Password));
        r2.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_Bad_Credentials_Returns401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("nobody@test.local", "wrong"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
