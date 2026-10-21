using ConCancha.Domain.Customers;

namespace ConCancha.Tests.Domain;

public class PhoneNormalizerTests
{
    private const string Expected = "5491112345678";

    [Theory]
    [InlineData("+54 9 11 1234-5678")]
    [InlineData("5491112345678")]
    [InlineData("541112345678")]
    [InlineData("11 1234 5678")]
    [InlineData("011 1234-5678")]
    [InlineData("011 15 1234 5678")]
    [InlineData("11 15 1234 5678")]
    [InlineData("(011) 15-1234-5678")]
    [InlineData("0054 9 11 1234 5678")]
    public void Normalize_DifferentWaysOfWritingTheSameNumber_GiveTheSameResult(string input)
    {
        Assert.Equal(Expected, PhoneNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("351 15 123 4567", "5493511234567")]
    [InlineData("0351 15 123 4567", "5493511234567")]
    public void Normalize_ThreeDigitAreaCode_DropsTheLocalFifteen(string input, string expected)
    {
        Assert.Equal(expected, PhoneNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123456")]
    [InlineData("11 1234")]
    [InlineData("+1 415 555 2671")]
    [InlineData("54911123456789012")]
    public void Normalize_NotAnArgentineMobile_ReturnsNull(string? input)
    {
        Assert.Null(PhoneNormalizer.Normalize(input));
    }
}
