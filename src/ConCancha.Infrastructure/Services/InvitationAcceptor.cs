using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Identity;
using ConCancha.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

public sealed class InvitationAcceptor(
    ConCanchaDbContext db,
    UserManager<AppUser> users,
    SessionFactory sessionFactory,
    TimeProvider clock,
    ILogger<InvitationAcceptor> logger) : IInvitationAcceptor
{
    private const string TokenField = "token";

    public async Task<Result<SessionResponse>> AcceptAsync(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var invitation = await FindPendingInvitationAsync(email, request.Token, cancellationToken).ConfigureAwait(false);
        if (invitation is null)
            return Result<SessionResponse>.Failure(ServiceError.Validation(TokenField, TeamMessages.InvalidInvitation));

        if (await users.FindByEmailAsync(email).ConfigureAwait(false) is not null)
            return Result<SessionResponse>.Failure(ServiceError.Conflict(TeamMessages.InvitedAccountExists));

        return await CreateAccountAndMembershipAsync(invitation, request.Password, cancellationToken).ConfigureAwait(false);
    }

    private Task<ClubInvitation?> FindPendingInvitationAsync(string email, string token, CancellationToken cancellationToken)
    {
        var hash = ManagementToken.Hash(token);
        var now = clock.GetUtcNow();

        return db.ClubInvitations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                i => i.TokenHash == hash && i.Email == email && i.Status == InvitationStatus.Pending && i.ExpiresAt > now,
                cancellationToken);
    }

    private async Task<Result<SessionResponse>> CreateAccountAndMembershipAsync(
        ClubInvitation invitation, string password, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var user = new AppUser { UserName = invitation.Email, Email = invitation.Email, EmailConfirmed = true };

        var creation = await users.CreateAsync(user, password).ConfigureAwait(false);
        if (!creation.Succeeded)
            return Result<SessionResponse>.Failure(
                IdentityErrorMapper.ToValidationError(creation, nameof(AcceptInvitationRequest.Password)));

        db.ClubMembers.Add(new ClubMember { ClubId = invitation.ClubId, UserId = user.Id, Role = invitation.Role });
        invitation.Status = InvitationStatus.Accepted;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Invitation {InvitationId} accepted by user {UserId}", invitation.Id, user.Id);
        return await BuildSessionAsync(user, invitation, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<SessionResponse>> BuildSessionAsync(
        AppUser user, ClubInvitation invitation, CancellationToken cancellationToken)
    {
        var club = await db.Clubs.AsNoTracking().FirstAsync(c => c.Id == invitation.ClubId, cancellationToken).ConfigureAwait(false);
        return Result<SessionResponse>.Success(sessionFactory.Create(user, club, invitation.Role));
    }
}
