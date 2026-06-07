using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Services;
using FluentAssertions;
using Xunit;

namespace ExpenseTracker.Domain.Tests.Services;

public class SavingsRateClassifierTests
{
    [Theory]
    [InlineData(1000, 700, StatusColor.Green)]      // 30%
    [InlineData(1000, 750, StatusColor.Orange)]     // 25%
    [InlineData(1000, 800, StatusColor.OrangeRedTint)] // 20% → OrangeRedTint
    [InlineData(1000, 900, StatusColor.OrangeRedTint)] // 10%
    [InlineData(1000, 950, StatusColor.BloodRed)]   // 5%
    [InlineData(0, 100, StatusColor.BloodRed)]
    [InlineData(1000, 1000, StatusColor.BloodRed)]  // 0%
    public void Classify_ReturnsExpectedColor(decimal income, decimal expense, StatusColor expected)
    {
        SavingsRateClassifier.Classify(income, expense).Should().Be(expected);
    }
}
