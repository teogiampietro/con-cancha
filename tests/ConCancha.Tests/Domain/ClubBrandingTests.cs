using ConCancha.Domain.Clubs;

namespace ConCancha.Tests.Domain;

public class ClubBrandingTests
{
    [Theory]
    [InlineData("#B8341E")]
    [InlineData("#6DFF7D")]
    [InlineData("#FFFFFF")]
    [InlineData("#3F7D5A")]
    public void Validate_AcceptsReadableColors(string color) => Assert.Null(ClubBranding.Validate(color));

    [Fact]
    public void Validate_AcceptsTheDefaults()
    {
        Assert.Null(ClubBranding.Validate(ClubBranding.DefaultPrimaryColor));
        Assert.Null(ClubBranding.Validate(ClubBranding.DefaultSecondaryColor));
    }

    [Theory]
    [InlineData("")]
    [InlineData("red")]
    [InlineData("B8341E")]
    [InlineData("#B8341")]
    [InlineData("#B8341EE")]
    [InlineData("#GGGGGG")]
    public void Validate_RejectsWhatIsNotAHexColor(string color) => Assert.NotNull(ClubBranding.Validate(color));

    [Theory]
    [InlineData("#000000")]
    [InlineData("#111111")]
    [InlineData("#1A1A2E")]
    public void Validate_RejectsColorsThatDisappearOnTheDarkSite(string color) => Assert.NotNull(ClubBranding.Validate(color));

    [Theory]
    [InlineData(" #b8341e ", "#B8341E")]
    [InlineData(null, "")]
    public void Normalize_TrimsAndUppercases(string? input, string expected) => Assert.Equal(expected, ClubBranding.Normalize(input));

    [Theory]
    [InlineData("#6DFF7D", ClubBranding.DarkText)]
    [InlineData("#FFFFFF", ClubBranding.DarkText)]
    [InlineData("#B8341E", ClubBranding.LightText)]
    [InlineData("#3F7D5A", ClubBranding.LightText)]
    public void ReadableTextOn_PicksTheBetterContrast(string background, string expected) =>
        Assert.Equal(expected, ClubBranding.ReadableTextOn(background));

    [Theory]
    [InlineData("#6DFF7D")]
    [InlineData("#B8341E")]
    [InlineData("#808080")]
    [InlineData("#C0392B")]
    public void ReadableTextOn_AlwaysReachesAaForNormalText(string background)
    {
        var text = ClubBranding.ReadableTextOn(background);

        Assert.True(ClubBranding.ContrastRatio(text, background) >= 4.5);
    }

    [Fact]
    public void ContrastRatio_BlackOnWhiteIs21() =>
        Assert.Equal(21.0, ClubBranding.ContrastRatio("#000000", "#FFFFFF"), precision: 1);
}
