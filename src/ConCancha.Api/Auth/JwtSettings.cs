using System.ComponentModel.DataAnnotations;

namespace ConCancha.Api.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    private const int MinKeyLength = 32;
    private const int MinDurationMinutes = 5;
    private const int MaxDurationMinutes = 60 * 24 * 7;
    private const int DefaultDurationMinutes = 8 * 60;

    /// <summary>Signing key. Comes from configuration or the Jwt__Key variable; never from the repo.</summary>
    [Required, MinLength(MinKeyLength)] public string Key { get; set; } = string.Empty;
    [Required] public string Issuer { get; set; } = "concancha";
    [Required] public string Audience { get; set; } = "concancha";
    [Range(MinDurationMinutes, MaxDurationMinutes)] public int DurationMinutes { get; set; } = DefaultDurationMinutes;
}
