namespace ConCancha.Infrastructure.Identity;

/// <summary>
/// The platform admin's credentials, set in the host's environment and never in the repo. Only the password's hash is
/// stored: changing the hash is how the password is reset.
/// </summary>
public sealed class PlatformOptions
{
    public const string SectionName = "Platform";

    public string? AdminUser { get; set; }

    public string? AdminPasswordHash { get; set; }
}
