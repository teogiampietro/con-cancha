namespace ConCancha.Domain.Customers;

/// <summary>
/// Turns what a person types into one canonical Argentine mobile number (<c>549</c> + 10 digits), so the same
/// WhatsApp written in different ways identifies the same customer.
/// </summary>
public static class PhoneNormalizer
{
    private const string CountryPrefix = "54";
    private const string MobilePrefix = "549";
    private const string InternationalCallPrefix = "00";
    private const string TrunkPrefix = "0";
    private const string MobileInfix = "15";
    private const int SubscriberDigitsWithAreaCode = 10;
    private const int NormalizedLength = 13;
    private const int MinAreaCodeLength = 2;
    private const int MaxAreaCodeLength = 4;

    public static string? Normalize(string? input)
    {
        var digits = new string((input ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith(InternationalCallPrefix, StringComparison.Ordinal)) digits = digits[InternationalCallPrefix.Length..];

        var normalized = digits.StartsWith(CountryPrefix, StringComparison.Ordinal)
            ? NormalizeWithCountry(digits)
            : NormalizeLocal(digits);

        return normalized is { Length: NormalizedLength } ? normalized : null;
    }

    private static string NormalizeWithCountry(string digits) =>
        digits.StartsWith(MobilePrefix, StringComparison.Ordinal) ? digits : MobilePrefix + digits[CountryPrefix.Length..];

    private static string NormalizeLocal(string digits)
    {
        if (digits.StartsWith(TrunkPrefix, StringComparison.Ordinal)) digits = digits[TrunkPrefix.Length..];
        return MobilePrefix + RemoveMobileInfix(digits);
    }

    /// <summary>"11 15 1234 5678" is the same number as "11 1234 5678": drop the local "15" after the area code.</summary>
    private static string RemoveMobileInfix(string digits)
    {
        if (digits.Length != SubscriberDigitsWithAreaCode + MobileInfix.Length) return digits;

        for (var areaCodeLength = MinAreaCodeLength; areaCodeLength <= MaxAreaCodeLength; areaCodeLength++)
        {
            if (string.CompareOrdinal(digits, areaCodeLength, MobileInfix, 0, MobileInfix.Length) == 0)
                return digits.Remove(areaCodeLength, MobileInfix.Length);
        }
        return digits;
    }
}
