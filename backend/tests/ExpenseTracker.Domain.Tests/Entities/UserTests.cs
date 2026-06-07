using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Domain.Tests.Entities;

public class UserTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_NormalisesCaseAndTrimming()
    {
        var u = User.Create("  Jane  ", "JANE@EX.COM", "+15551234567", "h", "usd", Now);

        u.FullName.Should().Be("Jane");
        u.Email.Should().Be("jane@ex.com");
        u.CurrencyCode.Should().Be("USD");
        u.Role.Should().Be(UserRole.User);
        u.IsActive.Should().BeTrue();
        u.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "e@x.com", "+15551234567", "h", "USD")]
    [InlineData("Jane", "", "+15551234567", "h", "USD")]
    [InlineData("Jane", "e@x.com", "", "h", "USD")]
    [InlineData("Jane", "e@x.com", "+15551234567", "", "USD")]
    [InlineData("Jane", "e@x.com", "+15551234567", "h", "")]
    public void Create_RejectsBlankRequiredFields(string name, string email, string phone, string hash, string currency)
    {
        var act = () => User.Create(name, email, phone, hash, currency, Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProfile_AppliesNonBlankValues()
    {
        var u = User.Create("Jane", "j@x.com", "+15551234567", "h", "USD", Now);
        u.UpdateProfile(" Jane Doe ", "+15559999999", "eur", Now.AddMinutes(1));

        u.FullName.Should().Be("Jane Doe");
        u.Phone.Should().Be("+15559999999");
        u.CurrencyCode.Should().Be("EUR");
    }

    [Fact]
    public void ChangeRole_UpdatesRoleAndTimestamp()
    {
        var u = User.Create("Jane", "j@x.com", "+15551234567", "h", "USD", Now);
        var later = Now.AddDays(1);

        u.ChangeRole(UserRole.Admin, later);

        u.Role.Should().Be(UserRole.Admin);
        u.UpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void Deactivate_SetsInactive_ButNotDeleted()
    {
        var u = User.Create("Jane", "j@x.com", "+15551234567", "h", "USD", Now);
        u.Deactivate(Now.AddMinutes(1));

        u.IsActive.Should().BeFalse();
        u.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_RestoresActive()
    {
        var u = User.Create("Jane", "j@x.com", "+15551234567", "h", "USD", Now);
        u.Deactivate(Now);
        u.Activate(Now.AddMinutes(1));

        u.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_SetsAllDeletedFlags()
    {
        var u = User.Create("Jane", "j@x.com", "+15551234567", "h", "USD", Now);
        u.SoftDelete(Now.AddMinutes(1));

        u.IsActive.Should().BeFalse();
        u.IsDeleted.Should().BeTrue();
        u.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void SetPasswordHash_RejectsBlank()
    {
        var u = User.Create("Jane", "j@x.com", "+15551234567", "h", "USD", Now);

        var act = () => u.SetPasswordHash("", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPasswordHash_StoresNewHash()
    {
        var u = User.Create("Jane", "j@x.com", "+15551234567", "old", "USD", Now);
        u.SetPasswordHash("new", Now.AddMinutes(1));

        u.PasswordHash.Should().Be("new");
    }
}
