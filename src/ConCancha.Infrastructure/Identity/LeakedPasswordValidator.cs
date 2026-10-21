using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConCancha.Infrastructure.Identity;

/// <summary>
/// Rejects passwords found in known leaks (Have I Been Pwned, k-anonymity range: only the first 5 characters
/// of the SHA-1 hash are sent). If the service does not answer, the password is accepted.
/// </summary>
public sealed class LeakedPasswordValidator(
    IHttpClientFactory httpClientFactory,
    IOptions<AuthOptions> options,
    ILogger<LeakedPasswordValidator> logger) : IPasswordValidator<AppUser>
{
    public const string HttpClientName = "hibp";

    private const int HashPrefixLength = 5;
    private const char CountSeparator = ':';
    private const string NoOccurrences = "0";
    private const string LeakedPasswordCode = "LeakedPassword";
    private const string LeakedPasswordMessage = "Esa contraseña aparece en listas de contraseñas filtradas. Elegí otra.";

    public async Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        if (string.IsNullOrEmpty(password) || !options.Value.CheckLeakedPasswords)
            return IdentityResult.Success;

        // Identity's validator contract carries no cancellation token.
        var isLeaked = await IsLeakedOrFalseOnFailureAsync(password, CancellationToken.None).ConfigureAwait(false);

        return isLeaked
            ? IdentityResult.Failed(new IdentityError { Code = LeakedPasswordCode, Description = LeakedPasswordMessage })
            : IdentityResult.Success;
    }

    private async Task<bool> IsLeakedOrFalseOnFailureAsync(string password, CancellationToken cancellationToken)
    {
        try
        {
            return await IsLeakedAsync(password, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Leaked-password service unavailable; accepting the password.");
            return false;
        }
    }

    private async Task<bool> IsLeakedAsync(string password, CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        var suffix = hash[HashPrefixLength..];

        using var client = httpClientFactory.CreateClient(HttpClientName);
        var response = await client.GetStringAsync($"range/{hash[..HashPrefixLength]}", cancellationToken).ConfigureAwait(false);

        return response
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().Split(CountSeparator))
            .Any(parts => parts.Length == 2
                          && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase)
                          && parts[1] != NoOccurrences);
    }
}
