using System.Text;
using ConCancha.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ConCancha.Api.Auth;

/// <summary>Builds JWT validation from already-resolved settings, so tests can override them.</summary>
public sealed class JwtBearerOptionsSetup(IOptions<JwtSettings> settings) : IConfigureNamedOptions<JwtBearerOptions>
{
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(1);

    public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        var jwt = settings.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = AllowedClockSkew,
            NameClaimType = SessionClaims.Email,
            RoleClaimType = SessionClaims.Role,
        };
    }
}
