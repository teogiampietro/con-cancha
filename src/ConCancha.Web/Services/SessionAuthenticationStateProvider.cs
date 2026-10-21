using System.Security.Claims;
using ConCancha.Contracts;
using ConCancha.Contracts.Accounts;
using Microsoft.AspNetCore.Components.Authorization;

namespace ConCancha.Web.Services;

public sealed class SessionAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string AuthenticationType = "jwt";

    private readonly CurrentSession _session;

    public SessionAuthenticationStateProvider(CurrentSession session)
    {
        _session = session;
        _session.Changed += () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // The framework override carries no cancellation token.
        await _session.LoadAsync(CancellationToken.None);

        var identity = _session.Current is { } session
            ? new ClaimsIdentity(BuildClaims(session), AuthenticationType)
            : new ClaimsIdentity();

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static List<Claim> BuildClaims(SessionResponse session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.User.Id),
            new(ClaimTypes.Name, session.User.Email),
        };
        if (session.Role is { } role) claims.Add(new Claim(ClaimTypes.Role, role));
        if (session.Club is { } club) claims.Add(new Claim(SessionClaims.ClubId, club.Id.ToString()));
        return claims;
    }
}
