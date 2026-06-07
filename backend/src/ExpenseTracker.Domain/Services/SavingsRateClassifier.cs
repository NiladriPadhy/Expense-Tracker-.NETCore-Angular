using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Services;

/// <summary>
/// Maps savings-rate percentage to <see cref="StatusColor"/>.
/// Bands per research R-005 and spec FR-022/023.
/// </summary>
public static class SavingsRateClassifier
{
    public static StatusColor Classify(decimal totalIncome, decimal totalExpense)
    {
        if (totalIncome <= 0m)
        {
            return StatusColor.BloodRed;
        }
        var rate = ((totalIncome - totalExpense) / totalIncome) * 100m;
        return ClassifyRate(rate);
    }

    public static StatusColor ClassifyRate(decimal savingsRatePct)
    {
        if (savingsRatePct < 10m)
        {
            return StatusColor.BloodRed;
        }
        if (savingsRatePct <= 20m)
        {
            return StatusColor.OrangeRedTint;
        }
        if (savingsRatePct < 30m)
        {
            return StatusColor.Orange;
        }
        return StatusColor.Green;
    }

    public static decimal ComputeRate(decimal totalIncome, decimal totalExpense)
    {
        if (totalIncome <= 0m)
        {
            return 0m;
        }
        return decimal.Round(((totalIncome - totalExpense) / totalIncome) * 100m, 2, MidpointRounding.ToEven);
    }
}
