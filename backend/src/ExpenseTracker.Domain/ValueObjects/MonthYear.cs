namespace ExpenseTracker.Domain.ValueObjects;

public readonly record struct MonthYear : IComparable<MonthYear>
{
    public int Year { get; }
    public int Month { get; }

    public MonthYear(int year, int month)
    {
        if (year < 1900 || year > 2999)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year out of range.");
        }

        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be 1-12.");
        }

        Year = year;
        Month = month;
    }

    public static MonthYear From(DateTime utc) => new(utc.Year, utc.Month);

    public static MonthYear From(DateOnly date) => new(date.Year, date.Month);

    public MonthYear Next()
        => Month == 12 ? new MonthYear(Year + 1, 1) : new MonthYear(Year, Month + 1);

    public MonthYear Previous()
        => Month == 1 ? new MonthYear(Year - 1, 12) : new MonthYear(Year, Month - 1);

    public int CompareTo(MonthYear other)
    {
        var y = Year.CompareTo(other.Year);
        return y != 0 ? y : Month.CompareTo(other.Month);
    }

    public static bool operator <(MonthYear a, MonthYear b) => a.CompareTo(b) < 0;
    public static bool operator >(MonthYear a, MonthYear b) => a.CompareTo(b) > 0;
    public static bool operator <=(MonthYear a, MonthYear b) => a.CompareTo(b) <= 0;
    public static bool operator >=(MonthYear a, MonthYear b) => a.CompareTo(b) >= 0;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
