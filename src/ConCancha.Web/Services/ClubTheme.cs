using ConCancha.Domain.Clubs;

namespace ConCancha.Web.Services;

/// <summary>Turns a club's colors into the CSS variables of its public site.</summary>
public static class ClubTheme
{
    public static string Style(string primaryColor, string secondaryColor)
    {
        var primary = SafeColor(primaryColor, ClubBranding.DefaultPrimaryColor);
        var secondary = SafeColor(secondaryColor, ClubBranding.DefaultSecondaryColor);
        return $"--acc:{primary};--onacc:{ClubBranding.ReadableTextOn(primary)};--club2:{secondary}";
    }

    private static string SafeColor(string color, string fallback)
    {
        var normalized = ClubBranding.Normalize(color);
        return ClubBranding.Validate(normalized) is null ? normalized : fallback;
    }
}
