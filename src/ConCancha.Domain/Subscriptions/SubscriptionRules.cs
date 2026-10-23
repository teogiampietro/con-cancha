namespace ConCancha.Domain.Subscriptions;

/// <summary>
/// The trial is counted per club, from the end of its setup. A payment covers 30 days and then the subscription renews
/// by itself, so a club loses access only when its trial ends without a subscription.
/// </summary>
public static class SubscriptionRules
{
    public const int TrialDays = 14;
    public const int PaidDays = 30;
    public const int MinExtensionDays = 1;
    public const int MaxExtensionDays = 365;

    public static DateTimeOffset TrialEndsAfterSetup(DateTimeOffset setupCompletedAt) => setupCompletedAt.AddDays(TrialDays);

    public static SubscriptionStatus StatusOf(DateTimeOffset? trialEndsAt, DateTimeOffset? paidUntil, DateTimeOffset now)
    {
        if (paidUntil is not null) return paidUntil > now ? SubscriptionStatus.Subscribed : SubscriptionStatus.PaymentPending;
        if (trialEndsAt is null) return SubscriptionStatus.TrialNotStarted;

        return trialEndsAt > now ? SubscriptionStatus.Trial : SubscriptionStatus.TrialExpired;
    }

    public static bool HasAccess(SubscriptionStatus status) => status != SubscriptionStatus.TrialExpired;

    public static bool NeedsRegularization(SubscriptionStatus status) =>
        status is SubscriptionStatus.TrialExpired or SubscriptionStatus.PaymentPending;

    /// <returns>Days left rounded up; <c>null</c> before the trial starts.</returns>
    public static int? TrialDaysLeft(DateTimeOffset? trialEndsAt, DateTimeOffset now)
    {
        if (trialEndsAt is not { } end) return null;

        var remaining = end - now;
        return remaining <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(remaining.TotalDays);
    }

    public static bool IsValidExtension(int days) => days is >= MinExtensionDays and <= MaxExtensionDays;

    /// <summary>Extra days count from today when the trial is already over.</summary>
    public static DateTimeOffset ExtendTrial(DateTimeOffset? trialEndsAt, int days, DateTimeOffset now) =>
        LaterOf(trialEndsAt, now).AddDays(days);

    /// <summary>A payment covers a month from today, or from the end of the month already paid if that is still ahead.</summary>
    public static DateTimeOffset MarkPaid(DateTimeOffset? paidUntil, DateTimeOffset now) =>
        LaterOf(paidUntil, now).AddDays(PaidDays);

    private static DateTimeOffset LaterOf(DateTimeOffset? date, DateTimeOffset now) =>
        date is { } value && value > now ? value : now;
}
