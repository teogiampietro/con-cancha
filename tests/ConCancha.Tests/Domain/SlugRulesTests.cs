using ConCancha.Domain.Clubs;

namespace ConCancha.Tests.Domain;

public class SlugRulesTests
{
    [Theory]
    [InlineData("Club Los Pinos", "club-los-pinos")]
    [InlineData("Ñandú & Co.", "nandu-co")]
    [InlineData("  Pádel   Rosario  ", "padel-rosario")]
    [InlineData("---", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_BuildsSlugFromText(string? input, string expected) =>
        Assert.Equal(expected, SlugRules.Normalize(input));

    [Fact]
    public void Normalize_CutsAtMaxLengthWithoutTrailingDash()
    {
        var slug = SlugRules.Normalize(new string('a', SlugRules.MaxLength - 1) + " bbbbbbbb");

        Assert.True(slug.Length <= SlugRules.MaxLength);
        Assert.False(slug.EndsWith('-'));
    }

    [Theory]
    [InlineData("los-pinos")]
    [InlineData("abc")]
    [InlineData("club-2024")]
    public void Validate_AcceptsValidSlugs(string slug) => Assert.Null(SlugRules.Validate(slug));

    [Theory]
    [InlineData("ab", "al menos")]
    [InlineData("", "al menos")]
    [InlineData("Los-Pinos", "Solo letras")]
    [InlineData("-los-pinos", "Solo letras")]
    [InlineData("los-pinos-", "Solo letras")]
    [InlineData("los--pinos", "Solo letras")]
    [InlineData("los pinos", "Solo letras")]
    [InlineData("panel", "reservado")]
    [InlineData("api", "reservado")]
    [InlineData("swagger", "reservado")]
    public void Validate_RejectsInvalidSlugs(string slug, string expectedReason) =>
        Assert.Contains(expectedReason, SlugRules.Validate(slug));

    [Fact]
    public void Validate_RejectsTooLongSlugs() =>
        Assert.Contains("Máximo", SlugRules.Validate(new string('a', SlugRules.MaxLength + 1)));
}
