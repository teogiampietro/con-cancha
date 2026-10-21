using System.Text;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts;
using ConCancha.Domain.Clubs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ConCancha.Api.Auth;

public sealed class SessionTokenIssuer(IOptions<JwtSettings> settings, TimeProvider clock) : ISessionTokenIssuer
{
    public IssuedToken Issue(string userId, string email, Guid? clubId, ClubRole? role)
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
            Claims = BuildClaims(userId, email, clubId, role),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256),
        };

        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    private static Dictionary<string, object> BuildClaims(string userId, string email, Guid? clubId, ClubRole? role)
    {
        var claims = new Dictionary<string, object>
        {
            [SessionClaims.UserId] = userId,
            [SessionClaims.Email] = email,
        };
        if (clubId is { } id) claims[SessionClaims.ClubId] = id.ToString();
        if (role is { } clubRole) claims[SessionClaims.Role] = clubRole.ToString();
        return claims;
    }
}
