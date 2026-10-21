using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;

namespace ConCancha.Application.Abstractions;

public interface IClubService
{
    Task<Result<SessionResponse>> CreateClubForUserAsync(
        string userId, CreateClubRequest request, CancellationToken cancellationToken);

    Task<SlugAvailabilityResponse> GetSlugAvailabilityAsync(string slug, CancellationToken cancellationToken);

    Task<Result<ClubDto>> GetCurrentClubAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ClubMemberDto>> GetCurrentClubTeamAsync(CancellationToken cancellationToken);

    Task<Result<ClubDto>> UpdateHoursAsync(UpdateClubHoursRequest request, CancellationToken cancellationToken);

    Task<Result<PublicClubDto>> ResolvePublicClubBySlugAsync(string slug, CancellationToken cancellationToken);
}
