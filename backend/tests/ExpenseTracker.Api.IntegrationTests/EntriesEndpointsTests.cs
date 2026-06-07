using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Application.Auth.Dtos;
using ExpenseTracker.Application.Dashboard.Dtos;
using ExpenseTracker.Application.Entries.Dtos;
using ExpenseTracker.Application.Months.Dtos;
using ExpenseTracker.Domain.Common;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Api.IntegrationTests;

[Collection(nameof(IntegrationCollection))]
public class EntriesEndpointsTests
{
    private readonly TestWebApplicationFactory _factory;

    public EntriesEndpointsTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateEntry_FreeTextCategory_AndListByMonth()
    {
        var (client, _) = await RegisterAndAuthenticateAsync(_factory);

        var entry = new
        {
            entryDate = "2026-06-10",
            type = "Expense",
            amount = 12.50m,
            categoryId = (Guid?)null,
            categoryFreeText = "Coffee",
            note = "morning",
        };
        var post = await client.PostAsJsonAsync("/api/v1/entries", entry);
        post.StatusCode.Should().Be(HttpStatusCode.Created);

        var month = await client.GetFromJsonAsync<MonthlyViewDto>("/api/v1/months/2026/6", JsonOpts());
        month.Should().NotBeNull();
        month!.TotalExpense.Should().Be(12.50m);
        month.Entries.Should().ContainSingle().Which.CategoryName.Should().Be("Coffee");
    }

    [Fact]
    public async Task CreateEntry_FutureMonth_Returns400WithCode()
    {
        var (client, _) = await RegisterAndAuthenticateAsync(_factory);
        var futureYear = DateTime.UtcNow.Year + 1;

        var entry = new
        {
            entryDate = $"{futureYear}-12-01",
            type = "Income",
            amount = 100m,
            categoryFreeText = "Bonus",
        };
        var post = await client.PostAsJsonAsync("/api/v1/entries", entry);
        post.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await post.Content.ReadAsStringAsync();
        body.Should().Contain("future_month_write_forbidden");
    }

    [Fact]
    public async Task UpdateEntry_AmountAndCategory_RecomputesMonth()
    {
        var (client, _) = await RegisterAndAuthenticateAsync(_factory);

        var created = await client.PostAsJsonAsync("/api/v1/entries", new
        {
            entryDate = "2026-06-15",
            type = "Income",
            amount = 1000m,
            categoryFreeText = "Salary",
        });
        created.EnsureSuccessStatusCode();
        var entryDto = await created.Content.ReadFromJsonAsync<EntryDto>(JsonOpts());

        var update = await client.PutAsJsonAsync($"/api/v1/entries/{entryDto!.Id}", new
        {
            entryDate = "2026-06-16",
            type = "Income",
            amount = 1500m,
            categoryFreeText = "Salary bump",
        });
        update.EnsureSuccessStatusCode();

        var month = await client.GetFromJsonAsync<MonthlyViewDto>("/api/v1/months/2026/6", JsonOpts());
        month!.TotalIncome.Should().Be(1500m);
    }

    [Fact]
    public async Task DeleteEntry_Removes_AndZerosMonth()
    {
        var (client, _) = await RegisterAndAuthenticateAsync(_factory);

        var created = await client.PostAsJsonAsync("/api/v1/entries", new
        {
            entryDate = "2026-06-20",
            type = "Expense",
            amount = 50m,
            categoryFreeText = "Books",
        });
        var entryDto = await created.Content.ReadFromJsonAsync<EntryDto>(JsonOpts());

        var del = await client.DeleteAsync($"/api/v1/entries/{entryDto!.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var month = await client.GetFromJsonAsync<MonthlyViewDto>("/api/v1/months/2026/6", JsonOpts());
        month!.TotalExpense.Should().Be(0m);
        month.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEntry_OtherUser_Returns403()
    {
        var (clientA, _) = await RegisterAndAuthenticateAsync(_factory);
        var (clientB, _) = await RegisterAndAuthenticateAsync(_factory);

        var created = await clientA.PostAsJsonAsync("/api/v1/entries", new
        {
            entryDate = "2026-06-05",
            type = "Expense",
            amount = 9m,
            categoryFreeText = "Tea",
        });
        var entryDto = await created.Content.ReadFromJsonAsync<EntryDto>(JsonOpts());

        var resp = await clientB.GetAsync($"/api/v1/entries/{entryDto!.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnauthenticatedRequest_Returns401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/months/2026/6");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- helpers ----------

    internal static async Task<(HttpClient Client, AuthResult Auth)> RegisterAndAuthenticateAsync(TestWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var req = new RegisterUserRequest(
            "Test User",
            $"u{Guid.NewGuid():N}@itest.local",
            $"+1415555{Random.Shared.Next(1000, 9999)}",
            "Password123!",
            "USD");
        var resp = await client.PostAsJsonAsync("/api/v1/auth/register-json", req);
        resp.EnsureSuccessStatusCode();
        var auth = await resp.Content.ReadFromJsonAsync<AuthResult>(JsonOpts());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (client, auth);
    }

    internal static JsonSerializerOptions JsonOpts() => new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}

[Collection(nameof(IntegrationCollection))]
public class MonthsEndpointsTests
{
    private readonly TestWebApplicationFactory _factory;
    public MonthsEndpointsTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task MonthView_FutureMonth_IsReadOnly_AndProjectsOpeningBalance()
    {
        var (client, _) = await EntriesEndpointsTests.RegisterAndAuthenticateAsync(_factory);

        var current = DateTime.UtcNow;
        var post = await client.PostAsJsonAsync("/api/v1/entries", new
        {
            entryDate = $"{current.Year:D4}-{current.Month:D2}-{Math.Min(current.Day, 28):D2}",
            type = "Income",
            amount = 2500m,
            categoryFreeText = "Salary",
        });
        post.EnsureSuccessStatusCode();

        var futureMonth = current.AddMonths(1);
        var resp = await client.GetAsync($"/api/v1/months/{futureMonth.Year}/{futureMonth.Month}");
        resp.EnsureSuccessStatusCode();
        var view = await resp.Content.ReadFromJsonAsync<MonthlyViewDto>(EntriesEndpointsTests.JsonOpts());
        view!.ReadOnly.Should().BeTrue();
        view.OpeningBalance.Should().Be(2500m);
        view.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task MonthView_InvalidMonth_Returns400()
    {
        var (client, _) = await EntriesEndpointsTests.RegisterAndAuthenticateAsync(_factory);
        var resp = await client.GetAsync("/api/v1/months/2026/13");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

[Collection(nameof(IntegrationCollection))]
public class DashboardEndpointsTests
{
    private readonly TestWebApplicationFactory _factory;
    public DashboardEndpointsTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_AfterIncomeAndExpense_ReportsRateAndColor()
    {
        var (client, _) = await EntriesEndpointsTests.RegisterAndAuthenticateAsync(_factory);
        var now = DateTime.UtcNow;
        var iso = $"{now.Year:D4}-{now.Month:D2}-{Math.Min(now.Day, 28):D2}";

        await client.PostAsJsonAsync("/api/v1/entries", new
        {
            entryDate = iso, type = "Income", amount = 2000m, categoryFreeText = "Salary",
        });
        await client.PostAsJsonAsync("/api/v1/entries", new
        {
            entryDate = iso, type = "Expense", amount = 200m, categoryFreeText = "Food",
        });

        var dashboard = await client.GetFromJsonAsync<DashboardDto>("/api/v1/dashboard", EntriesEndpointsTests.JsonOpts());
        dashboard.Should().NotBeNull();
        dashboard!.CurrentMonthSavingsRatePct.Should().Be(90m);
        dashboard.CurrentMonthStatusColor.Should().Be(StatusColor.Green);
        dashboard.AlertExpenseExceedsIncome.Should().BeFalse();
        dashboard.Trend.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Dashboard_ExpenseExceedsIncome_RaisesAlert()
    {
        var (client, _) = await EntriesEndpointsTests.RegisterAndAuthenticateAsync(_factory);
        var now = DateTime.UtcNow;
        var iso = $"{now.Year:D4}-{now.Month:D2}-{Math.Min(now.Day, 28):D2}";

        await client.PostAsJsonAsync("/api/v1/entries", new
        {
            entryDate = iso, type = "Income", amount = 100m, categoryFreeText = "Salary",
        });
        await client.PostAsJsonAsync("/api/v1/entries", new
        {
            entryDate = iso, type = "Expense", amount = 500m, categoryFreeText = "Food",
        });

        var dashboard = await client.GetFromJsonAsync<DashboardDto>("/api/v1/dashboard", EntriesEndpointsTests.JsonOpts());
        dashboard!.AlertExpenseExceedsIncome.Should().BeTrue();
        dashboard.CurrentMonthStatusColor.Should().Be(StatusColor.BloodRed);
    }
}

[CollectionDefinition(nameof(IntegrationCollection))]
public class IntegrationCollection : ICollectionFixture<TestWebApplicationFactory>
{
}
