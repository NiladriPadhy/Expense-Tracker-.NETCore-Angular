using System.Text.RegularExpressions;

namespace ExpenseTracker.Domain.ValueObjects;

public readonly record struct PhoneNumber
{
    // E.164: + then 8-15 digits.
    private static readonly Regex Pattern = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    public static PhoneNumber Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException("Phone number cannot be empty.", nameof(raw));
        }

        var normalized = new string(raw.Where(static c => !char.IsWhiteSpace(c)).ToArray());
        if (!Pattern.IsMatch(normalized))
        {
            throw new ArgumentException($"Invalid E.164 phone number: '{raw}'.", nameof(raw));
        }

        return new PhoneNumber(normalized);
    }

    public static bool TryParse(string raw, out PhoneNumber phone)
    {
        try
        {
            phone = Parse(raw);
            return true;
        }
        catch
        {
            phone = default;
            return false;
        }
    }

    public override string ToString() => Value;
}
