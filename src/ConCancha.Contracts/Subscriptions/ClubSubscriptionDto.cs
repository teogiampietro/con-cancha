using ConCancha.Domain.Subscriptions;

namespace ConCancha.Contracts.Subscriptions;

/// <param name="HasAccess">Whether the club can use the panel right now.</param>
/// <param name="TrialDaysLeft">Days until the trial ends, rounded up; <c>null</c> before the trial starts.</param>
/// <param name="TrialEndsOn">Day the trial ends, in Argentina; <c>null</c> before the trial starts.</param>
/// <param name="PaidUntilOn">Last day covered by the payments; <c>null</c> for a club that never paid.</param>
public record ClubSubscriptionDto(
    SubscriptionStatus Status,
    bool HasAccess,
    int? TrialDaysLeft,
    DateOnly? TrialEndsOn,
    DateOnly? PaidUntilOn);
