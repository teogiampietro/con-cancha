using ConCancha.Application.Abstractions;
using ConCancha.Application.Emails;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Identity;
using ConCancha.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

public sealed class TeamService(
    ConCanchaDbContext db,
    UserManager<AppUser> users,
    ITenantContext tenant,
    IEmailSender emailSender,
    TeamLinks links,
    TimeProvider clock,
    ILogger<TeamService> logger) : ITeamService
{
    public async Task<IReadOnlyList<ClubMemberDto>> GetCurrentClubTeamAsync(CancellationToken cancellationToken)
    {
        var members = await db.ClubMembers
            .Join(db.Users, member => member.UserId, user => user.Id, (member, user) => new { Member = member, user.Email })
            .OrderBy(row => row.Member.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = clock.GetUtcNow();
        var invitations = await db.ClubInvitations
            .Where(invitation => invitation.Status == InvitationStatus.Pending && invitation.ExpiresAt > now)
            .OrderBy(invitation => invitation.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return members.Select(row => TeamMapper.ToDto(row.Member, row.Email ?? string.Empty))
            .Concat(invitations.Select(TeamMapper.ToDto))
            .ToList();
    }

    public async Task<Result<ClubMemberDto>> InviteAsync(InviteTeamMemberRequest request, CancellationToken cancellationToken)
    {
        if (tenant.ClubId is not { } clubId)
            return Result<ClubMemberDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await users.FindByEmailAsync(email).ConfigureAwait(false);
        if (await RejectIfNoRoomAsync(email, cancellationToken).ConfigureAwait(false) is { } full)
            return Result<ClubMemberDto>.Failure(full);

        return existing is null
            ? await InviteNewPersonAsync(clubId, email, cancellationToken).ConfigureAwait(false)
            : await AddExistingAccountAsync(clubId, existing, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> RemoveMemberAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await db.ClubMembers.FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken).ConfigureAwait(false);
        if (member is null)
            return Result.Failure(ServiceError.NotFound(TeamMessages.MemberNotFound));
        if (member.Role == ClubRole.Owner)
            return Result.Failure(ServiceError.Conflict(TeamMessages.CannotRemoveOwner));

        db.ClubMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Member {MemberId} removed from club {ClubId}", memberId, member.ClubId);
        return Result.Success();
    }

    public async Task<Result> RevokeInvitationAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var invitation = await db.ClubInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.Status == InvitationStatus.Pending, cancellationToken)
            .ConfigureAwait(false);
        if (invitation is null)
            return Result.Failure(ServiceError.NotFound(TeamMessages.InvitationNotFound));

        invitation.Status = InvitationStatus.Revoked;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<ServiceError?> RejectIfNoRoomAsync(string email, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var staff = await db.ClubMembers.CountAsync(member => member.Role == ClubRole.Staff, cancellationToken).ConfigureAwait(false);
        var pending = await db.ClubInvitations
            .CountAsync(i => i.Status == InvitationStatus.Pending && i.ExpiresAt > now && i.Email != email, cancellationToken)
            .ConfigureAwait(false);

        return TeamPolicy.HasRoomForAnother(staff + pending) ? null : ServiceError.Conflict(TeamMessages.TeamFull);
    }

    private async Task<Result<ClubMemberDto>> InviteNewPersonAsync(Guid clubId, string email, CancellationToken cancellationToken)
    {
        await RevokePendingAsync(email, cancellationToken).ConfigureAwait(false);
        var token = ManagementToken.Generate();
        var invitation = NewInvitation(clubId, email, token);
        db.ClubInvitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await SendInvitationAsync(clubId, invitation, token, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Invitation {InvitationId} created for club {ClubId}", invitation.Id, clubId);
        return Result<ClubMemberDto>.Success(TeamMapper.ToDto(invitation));
    }

    private async Task<Result<ClubMemberDto>> AddExistingAccountAsync(Guid clubId, AppUser user, CancellationToken cancellationToken)
    {
        if (await db.ClubMembers.AnyAsync(member => member.UserId == user.Id, cancellationToken).ConfigureAwait(false))
            return Result<ClubMemberDto>.Failure(ServiceError.Conflict(TeamMessages.AlreadyInTeam));

        await RevokePendingAsync(user.Email!, cancellationToken).ConfigureAwait(false);
        var member = new ClubMember { ClubId = clubId, UserId = user.Id, Role = ClubRole.Staff };
        db.ClubMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await SendMemberAddedAsync(clubId, user.Email!, cancellationToken).ConfigureAwait(false);
        return Result<ClubMemberDto>.Success(TeamMapper.ToDto(member, user.Email!));
    }

    private async Task RevokePendingAsync(string email, CancellationToken cancellationToken)
    {
        var pending = await db.ClubInvitations
            .Where(i => i.Email == email && i.Status == InvitationStatus.Pending)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var invitation in pending) invitation.Status = InvitationStatus.Revoked;
    }

    private ClubInvitation NewInvitation(Guid clubId, string email, string token) =>
        new()
        {
            ClubId = clubId,
            Email = email,
            TokenHash = ManagementToken.Hash(token),
            ExpiresAt = TeamPolicy.ExpiryFrom(clock.GetUtcNow()),
            CreatedAt = clock.GetUtcNow(),
        };

    private async Task SendInvitationAsync(Guid clubId, ClubInvitation invitation, string token, CancellationToken cancellationToken)
    {
        var clubName = await ClubNameAsync(clubId, cancellationToken).ConfigureAwait(false);
        var link = links.AcceptInvitationLink(invitation.Email, token);
        var mail = EmailTemplates.TeamInvitation(clubName, link, TeamPolicy.InvitationValidity.Days);
        await emailSender.SendAsync(invitation.Email, mail.Subject, mail.Body, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendMemberAddedAsync(Guid clubId, string email, CancellationToken cancellationToken)
    {
        var clubName = await ClubNameAsync(clubId, cancellationToken).ConfigureAwait(false);
        var mail = EmailTemplates.TeamMemberAdded(clubName, links.PanelLink());
        await emailSender.SendAsync(email, mail.Subject, mail.Body, cancellationToken).ConfigureAwait(false);
    }

    private Task<string> ClubNameAsync(Guid clubId, CancellationToken cancellationToken) =>
        db.Clubs.Where(club => club.Id == clubId).Select(club => club.Name).FirstAsync(cancellationToken);
}
