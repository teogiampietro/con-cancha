using ConCancha.Application.Abstractions;
using ConCancha.Application.Emails;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Subscriptions;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

public sealed class PlatformSubscriptionService(
    ConCanchaDbContext db,
    IEmailSender emailSender,
    TimeProvider clock,
    ILogger<PlatformSubscriptionService> logger) : IPlatformSubscriptionService
{
    private const string DaysField = "days";

    public async Task<Result> ExtendTrialAsync(Guid clubId, int days, CancellationToken cancellationToken)
    {
        if (!SubscriptionRules.IsValidExtension(days))
            return Result.Failure(ServiceError.Validation(DaysField, SubscriptionMessages.InvalidExtensionDays));

        var club = await FindClubAsync(clubId, cancellationToken).ConfigureAwait(false);
        if (club is null) return Result.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));
        if (club.TrialEndsAt is null) return Result.Failure(ServiceError.Conflict(SubscriptionMessages.TrialNotStarted));

        club.TrialEndsAt = SubscriptionRules.ExtendTrial(club.TrialEndsAt, days, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Trial of club {ClubId} extended by {Days} days", clubId, days);
        return Result.Success();
    }

    public async Task<Result> MarkPaidAsync(Guid clubId, CancellationToken cancellationToken)
    {
        var club = await FindClubAsync(clubId, cancellationToken).ConfigureAwait(false);
        if (club is null) return Result.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        club.PaidUntil = SubscriptionRules.MarkPaid(club.PaidUntil, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Payment of club {ClubId} recorded; covered until {PaidUntil}", clubId, club.PaidUntil);
        return Result.Success();
    }

    public async Task<Result> SendRegularizationEmailAsync(Guid clubId, CancellationToken cancellationToken)
    {
        var club = await FindClubAsync(clubId, cancellationToken).ConfigureAwait(false);
        if (club is null) return Result.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        var status = SubscriptionRules.StatusOf(club.TrialEndsAt, club.PaidUntil, clock.GetUtcNow());
        if (!SubscriptionRules.NeedsRegularization(status))
            return Result.Failure(ServiceError.Conflict(SubscriptionMessages.NothingToRegularize));

        var ownerEmail = await FindOwnerEmailAsync(clubId, cancellationToken).ConfigureAwait(false);
        if (ownerEmail is null) return Result.Failure(ServiceError.Conflict(SubscriptionMessages.OwnerWithoutEmail));

        var email = EmailTemplates.SubscriptionPending(club.Name);
        await emailSender.SendAsync(ownerEmail, email.Subject, email.Body, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Regularization email sent for club {ClubId}", clubId);
        return Result.Success();
    }

    private Task<Club?> FindClubAsync(Guid clubId, CancellationToken cancellationToken) =>
        db.Clubs.FirstOrDefaultAsync(club => club.Id == clubId, cancellationToken);

    /// <remarks>A club has one owner today; if it ever has more, the first one to join is written to.</remarks>
    private Task<string?> FindOwnerEmailAsync(Guid clubId, CancellationToken cancellationToken) =>
        db.ClubMembers.IgnoreQueryFilters().AsNoTracking()
            .Where(member => member.ClubId == clubId && member.Role == ClubRole.Owner)
            .Join(db.Users, member => member.UserId, user => user.Id, (member, user) => new { member.CreatedAt, user.Email })
            .OrderBy(owner => owner.CreatedAt)
            .Select(owner => owner.Email)
            .FirstOrDefaultAsync(cancellationToken);
}
