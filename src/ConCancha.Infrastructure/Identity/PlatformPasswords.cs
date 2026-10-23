using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace ConCancha.Infrastructure.Identity;

/// <summary>Hashing for the platform admin password, with ASP.NET's PBKDF2 format (salted, base64).</summary>
public static class PlatformPasswords
{
    private const int FingerprintLength = 16;

    // The hasher's type argument only labels the hash; it does not tie the hash to any entity.
    private static readonly PasswordHasher<PlatformOptions> Hasher = new();

    public static string Hash(string password) => Hasher.HashPassword(new PlatformOptions(), password);

    public static bool Verify(string passwordHash, string password) =>
        Hasher.VerifyHashedPassword(new PlatformOptions(), passwordHash, password) is not PasswordVerificationResult.Failed;

    /// <summary>A short digest of the hash. It goes into the admin token, so changing the hash invalidates it.</summary>
    public static string Fingerprint(string passwordHash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)))[..FingerprintLength];
}
