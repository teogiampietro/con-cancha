using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;

namespace ConCancha.Infrastructure.Services;

public static class TeamMapper
{
    public static ClubMemberDto ToDto(ClubMember member, string email) =>
        new(member.UserId, email, member.Role.ToString(), member.CreatedAt, member.Id, TeamMemberStatus.Active, ExpiresAt: null);

    public static ClubMemberDto ToDto(ClubInvitation invitation) =>
        new(string.Empty, invitation.Email, invitation.Role.ToString(), invitation.CreatedAt, invitation.Id,
            TeamMemberStatus.Invited, invitation.ExpiresAt);
}
