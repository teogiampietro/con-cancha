using ConCancha.Contracts.Subscriptions;
using ConCancha.Domain.Subscriptions;

namespace ConCancha.Infrastructure.Services;

public static class SubscriptionMapper
{
    public static ClubSubscriptionDto ToDto(DateTimeOffset? trialEndsAt, DateTimeOffset? paidUntil, DateTimeOffset now)
    {
        var status = SubscriptionRules.StatusOf(trialEndsAt, paidUntil, now);
        return new ClubSubscriptionDto(
            status,
            SubscriptionRules.HasAccess(status),
            SubscriptionRules.TrialDaysLeft(trialEndsAt, now),
            trialEndsAt is { } trialEnd ? LocalDay(trialEnd) : null,
            paidUntil is { } paid ? LocalDay(paid) : null);
    }

    private static DateOnly LocalDay(DateTimeOffset moment) => PlatformReporting.Zone.ToLocal(moment).Date;
}
