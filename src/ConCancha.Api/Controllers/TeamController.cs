using ConCancha.Api.RateLimiting;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConCancha.Api.Controllers;

public class TeamController(ITeamService team, IInvitationAcceptor invitations) : ApiControllerBase
{
    /// <summary>Club team: active people and pending invitations. Owner only.</summary>
    [HttpGet("api/club/team")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    public async Task<ActionResult<IReadOnlyList<ClubMemberDto>>> GetCurrentClubTeam(CancellationToken cancellationToken) =>
        Ok(await team.GetCurrentClubTeamAsync(cancellationToken));

    /// <summary>Adds an encargado by mail: an existing account joins right away, a new one gets an invitation link. Owner only.</summary>
    [HttpPost("api/club/team/invitations")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<ClubMemberDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Invite(InviteTeamMemberRequest request, CancellationToken cancellationToken) =>
        ToActionResult(
            await team.InviteAsync(request, cancellationToken),
            member => StatusCode(StatusCodes.Status201Created, member));

    /// <summary>Removes an encargado. The owner cannot be removed. Owner only.</summary>
    [HttpDelete("api/club/team/{memberId:guid}")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveMember(Guid memberId, CancellationToken cancellationToken) =>
        ToActionResult(await team.RemoveMemberAsync(memberId, cancellationToken), NoContent);

    /// <summary>Cancels a pending invitation. Owner only.</summary>
    [HttpDelete("api/club/team/invitations/{invitationId:guid}")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeInvitation(Guid invitationId, CancellationToken cancellationToken) =>
        ToActionResult(await team.RevokeInvitationAsync(invitationId, cancellationToken), NoContent);

    /// <summary>The invited person creates their password and enters the club as encargado.</summary>
    [HttpPost("api/auth/accept-invitation")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvitation(AcceptInvitationRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await invitations.AcceptAsync(request, cancellationToken), Ok);
}
