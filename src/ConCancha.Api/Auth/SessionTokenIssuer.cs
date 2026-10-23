using System.Text;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts;
using ConCancha.Contracts.Platform;
using ConCancha.Domain.Clubs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ConCancha.Api.Auth;

public sealed class SessionTokenIssuer(IOptions<JwtSettings> settings, TimeProvider clock) : ISessionTokenIssuer
{
    public IssuedToken Issue(string userId, string email, Guid? clubId, ClubRole? role) =>
        Create(BuildClaims(userId, email, clubId, role?.ToString()));

    public IssuedToken IssuePlatformAdmin(string credentialFingerprint) => Create(new Dictionary<string, object>
    {
        [SessionClaims.UserId] = PlatformRoles.Admin,
        [SessionClaims.Role] = PlatformRoles.Admin,
        [SessionClaims.PlatformCredential] = credentialFingerprint,
    });

    private IssuedToken Create(Dictionary<string, object> claims)
    {
        var jwt = settings.Value;
        var issuedAt = clock.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(jwt.DurationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256),
        };

        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    private static Dictionary<string, object> BuildClaims(string userId, string email, Guid? clubId, string? role)
    {
        var claims = new Dictionary<string, object>
        {
            [SessionClaims.UserId] = userId,
            [SessionClaims.Email] = email,
        };
        if (clubId is { } id) claims[SessionClaims.ClubId] = id.ToString();
        if (role is not null) claims[SessionClaims.Role] = role;
        return claims;
    }
}
