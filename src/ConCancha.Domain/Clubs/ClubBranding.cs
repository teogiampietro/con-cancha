using System.Globalization;
using System.Text.RegularExpressions;

namespace ConCancha.Domain.Clubs;

/// <summary>Colors a club can pick for its public site, and the legibility rules they must meet.</summary>
public static partial class ClubBranding
{
    public const string DefaultPrimaryColor = "#6DFF7D";
    public const string DefaultSecondaryColor = "#3F7D5A";
    public const string PublicSiteBackground = "#09090A";
    public const string LightText = "#FFFFFF";
    public const string DarkText = "#000000";

    /// <summary>WCAG 1.4.11: non-text elements and large text need at least 3:1 against the page.</summary>
    public const double MinContrastAgainstBackground = 3.0;

    private const int ChannelCount = 3;
    private const int HexDigitsPerChannel = 2;
    private const double LinearThreshold = 0.03928;
    private const double LinearDivisor = 12.92;
    private const double GammaOffset = 0.055;
    private const double GammaDivisor = 1.055;
    private const double GammaExponent = 2.4;
    private const double RedWeight = 0.2126;
    private const double GreenWeight = 0.7152;
    private const double BlueWeight = 0.0722;
    private const double LuminanceOffset = 0.05;
    private const double ChannelMax = 255.0;

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexFormat();

    public static string Normalize(string? color) => (color ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>Returns why the color is unusable, or <c>null</c> when valid. Expects a normalized value.</summary>
    public static string? Validate(string color)
    {
        if (!HexFormat().IsMatch(color)) return "Elegí un color válido, por ejemplo #B8341E.";
        if (ContrastRatio(color, PublicSiteBackground) < MinContrastAgainstBackground)
            return "Ese color es muy oscuro y se pierde sobre el fondo del sitio. Elegí uno más claro.";
        return null;
    }

    /// <summary>Text color (white or black) that reads best on top of <paramref name="background"/>.</summary>
    public static string ReadableTextOn(string background) =>
        ContrastRatio(LightText, background) >= ContrastRatio(DarkText, background) ? LightText : DarkText;

    public static double ContrastRatio(string firstColor, string secondColor)
    {
        var first = RelativeLuminance(firstColor);
        var second = RelativeLuminance(secondColor);
        return (Math.Max(first, second) + LuminanceOffset) / (Math.Min(first, second) + LuminanceOffset);
    }

    private static double RelativeLuminance(string color)
    {
        var channels = Enumerable.Range(0, ChannelCount).Select(index => ReadChannel(color, index)).ToArray();
        return RedWeight * channels[0] + GreenWeight * channels[1] + BlueWeight * channels[2];
    }

    private static double ReadChannel(string color, int index)
    {
        var hex = color.AsSpan(1 + index * HexDigitsPerChannel, HexDigitsPerChannel);
        var value = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / ChannelMax;
        return value <= LinearThreshold ? value / LinearDivisor : Math.Pow((value + GammaOffset) / GammaDivisor, GammaExponent);
    }
}
