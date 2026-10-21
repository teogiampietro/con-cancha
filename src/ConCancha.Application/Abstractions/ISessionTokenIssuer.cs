using ConCancha.Domain.Clubs;

namespace ConCancha.Application.Abstractions;

public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);

public interface ISessionTokenIssuer
{
    IssuedToken Issue(string userId, string email, Guid? clubId, ClubRole? role);
}
