namespace ConCancha.Domain.Clubs;

/// <summary>What counts as an acceptable club logo. The content decides, never the file name or declared type.</summary>
public static class LogoRules
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public const string FileTooLargeMessage = "El logo pesa más de 2 MB.";
    public const string NotAnImageMessage = "El logo tiene que ser una imagen PNG o JPG.";

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    public static LogoFormat? Detect(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(PngSignature)) return LogoFormat.Png;
        if (content.StartsWith(JpegSignature)) return LogoFormat.Jpeg;
        return null;
    }

    public static string Extension(LogoFormat format) => format == LogoFormat.Png ? "png" : "jpg";

    public static string ContentType(LogoFormat format) => format == LogoFormat.Png ? "image/png" : "image/jpeg";
}
