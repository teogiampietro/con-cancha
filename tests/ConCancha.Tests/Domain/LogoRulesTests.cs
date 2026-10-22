using ConCancha.Domain.Clubs;

namespace ConCancha.Tests.Domain;

public class LogoRulesTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];

    [Fact]
    public void Detect_RecognizesPng() => Assert.Equal(LogoFormat.Png, LogoRules.Detect(Png));

    [Fact]
    public void Detect_RecognizesJpeg() => Assert.Equal(LogoFormat.Jpeg, LogoRules.Detect(Jpeg));

    [Theory]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg'/>")]
    [InlineData("GIF89a")]
    [InlineData("")]
    public void Detect_RejectsAnythingElse(string text) => Assert.Null(LogoRules.Detect(System.Text.Encoding.UTF8.GetBytes(text)));

    [Fact]
    public void Detect_RejectsAPartialSignature() => Assert.Null(LogoRules.Detect(Png.AsSpan(0, 4)));
}
