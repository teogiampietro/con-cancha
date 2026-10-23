using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Platform;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Identity;

namespace ConCancha.Infrastructure.Services;

public sealed class SessionFactory(ISessionTokenIssuer tokenIssuer)
{
    public SessionResponse Create(AppUser user, Club? club, ClubRole? role)
    {
        var email = user.Email!;
        var token = tokenIssuer.Issue(user.Id, email, club?.Id, role);

        return new SessionResponse(
            token.Value,
            token.ExpiresAt,
            new UserDto(user.Id, email),
            club is null ? null : ClubMapper.ToDto(club),
            role?.ToString());
    }

    public SessionResponse CreatePlatformAdmin(string user, string credentialFingerprint)
    {
        var token = tokenIssuer.IssuePlatformAdmin(credentialFingerprint);
        return new SessionResponse(token.Value, token.ExpiresAt, new UserDto(PlatformRoles.Admin, user), Club: null, PlatformRoles.Admin);
    }
}
