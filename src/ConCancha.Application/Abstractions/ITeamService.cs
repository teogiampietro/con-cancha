using ConCancha.Application.Results;
using ConCancha.Contracts.Clubs;

namespace ConCancha.Application.Abstractions;

public interface ITeamService
{
    Task<IReadOnlyList<ClubMemberDto>> GetCurrentClubTeamAsync(CancellationToken cancellationToken);

    Task<Result<ClubMemberDto>> InviteAsync(InviteTeamMemberRequest request, CancellationToken cancellationToken);

    Task<Result> RemoveMemberAsync(Guid memberId, CancellationToken cancellationToken);

    Task<Result> RevokeInvitationAsync(Guid invitationId, CancellationToken cancellationToken);
}
