namespace ConCancha.Api.RateLimiting;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int AuthPerMinute { get; set; } = 20;
    public int QueryPerMinute { get; set; } = 120;
}
