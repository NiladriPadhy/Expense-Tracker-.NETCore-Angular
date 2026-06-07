using System.Text.RegularExpressions;

namespace ExpenseTracker.Domain.ValueObjects;

public readonly record struct EmailAddress
{
    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static EmailAddress Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !Pattern.IsMatch(raw.Trim()))
        {
            throw new ArgumentException($"Invalid email address: '{raw}'.", nameof(raw));
        }

        return new EmailAddress(raw.Trim().ToLowerInvariant());
    }

    public static bool TryParse(string raw, out EmailAddress email)
    {
        try
        {
            email = Parse(raw);
            return true;
        }
        catch
        {
            email = default;
            return false;
        }
    }

    public override string ToString() => Value;
}
