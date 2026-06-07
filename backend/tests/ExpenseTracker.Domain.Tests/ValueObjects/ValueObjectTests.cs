using ExpenseTracker.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Domain.Tests.ValueObjects;

public class EmailAddressTests
{
    [Theory]
    [InlineData("a@b.co")]
    [InlineData("Niladri.Test@Example.COM")]
    public void Parse_AcceptsValid(string raw)
    {
        var e = EmailAddress.Parse(raw);
        e.Value.Should().Be(raw.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    public void Parse_RejectsInvalid(string raw)
    {
        Action act = () => EmailAddress.Parse(raw);
        act.Should().Throw<ArgumentException>();
    }
}

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+14155552671")]
    [InlineData("+919876543210")]
    public void Parse_AcceptsE164(string raw)
    {
        var p = PhoneNumber.Parse(raw);
        p.Value.Should().Be(raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("4155552671")]
    [InlineData("+0123")]
    public void Parse_RejectsInvalid(string raw)
    {
        Action act = () => PhoneNumber.Parse(raw);
        act.Should().Throw<ArgumentException>();
    }
}

public class MonthYearTests
{
    [Fact]
    public void Next_RollsOverYear()
    {
        new MonthYear(2024, 12).Next().Should().Be(new MonthYear(2025, 1));
    }

    [Fact]
    public void Previous_RollsOverYear()
    {
        new MonthYear(2024, 1).Previous().Should().Be(new MonthYear(2023, 12));
    }

    [Fact]
    public void Comparable()
    {
        (new MonthYear(2024, 1) < new MonthYear(2024, 2)).Should().BeTrue();
        (new MonthYear(2025, 1) > new MonthYear(2024, 12)).Should().BeTrue();
    }
}
