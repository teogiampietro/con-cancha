using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ConCancha.Infrastructure.Identity;

/// <summary>
/// Checks the admin login against the configured user and hash. Without both settings nothing matches, so the panel
/// stays closed instead of open.
/// </summary>
public sealed class PlatformCredentials(IOptionsMonitor<PlatformOptions> options)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(options.CurrentValue.AdminUser) && !string.IsNullOrWhiteSpace(options.CurrentValue.AdminPasswordHash);

    /// <summary>The fingerprint of the hash in use, or <c>null</c> when the panel is not configured.</summary>
    public string? CurrentFingerprint => IsConfigured ? PlatformPasswords.Fingerprint(options.CurrentValue.AdminPasswordHash!) : null;

    /// <summary>
    /// Both parts are always evaluated, so a wrong user does not skip the slow hash check and reveal itself by timing.
    /// </summary>
    public bool Matches(string user, string password)
    {
        if (!IsConfigured) return false;

        var userMatches = SameText(options.CurrentValue.AdminUser!, user);
        var passwordMatches = PlatformPasswords.Verify(options.CurrentValue.AdminPasswordHash!, password);
        return userMatches & passwordMatches;
    }

    private static bool SameText(string expected, string given) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(given));
}
