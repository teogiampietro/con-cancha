using System.Security.Cryptography;
using System.Text;

namespace ConCancha.Infrastructure.Services;

/// <summary>Secret in a player's management link. Only its hash is stored, so a database leak does not expose links.</summary>
public static class ManagementToken
{
    private const int SecretBytes = 32;

    public static string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(SecretBytes)).ToLowerInvariant();

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
