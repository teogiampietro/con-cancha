using ConCancha.Domain.Subscriptions;

namespace ConCancha.Tests.Domain;

public class SubscriptionRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TheTrialStartsWhenTheSetupEndsAndLastsFourteenDays()
    {
        Assert.Equal(Now.AddDays(14), SubscriptionRules.TrialEndsAfterSetup(Now));
    }

    [Fact]
    public void AClubWithoutFinishedSetupHasNotStartedItsTrial()
    {
        Assert.Equal(SubscriptionStatus.TrialNotStarted, SubscriptionRules.StatusOf(null, null, Now));
    }

    [Fact]
    public void TheTrialIsOpenUntilItEndsAndExpiresAtThatInstant()
    {
        Assert.Equal(SubscriptionStatus.Trial, SubscriptionRules.StatusOf(Now.AddSeconds(1), null, Now));
        Assert.Equal(SubscriptionStatus.TrialExpired, SubscriptionRules.StatusOf(Now, null, Now));
    }

    [Fact]
    public void AnExpiredTrialWithoutPaymentHasNoAccess()
    {
        var status = SubscriptionRules.StatusOf(Now.AddDays(-1), null, Now);

        Assert.Equal(SubscriptionStatus.TrialExpired, status);
        Assert.False(SubscriptionRules.HasAccess(status));
        Assert.True(SubscriptionRules.NeedsRegularization(status));
    }

    [Fact]
    public void ASubscriptionThatRanOutRenewsAndKeepsAccess()
    {
        var status = SubscriptionRules.StatusOf(Now.AddDays(-20), Now.AddDays(-1), Now);

        Assert.Equal(SubscriptionStatus.PaymentPending, status);
        Assert.True(SubscriptionRules.HasAccess(status));
        Assert.True(SubscriptionRules.NeedsRegularization(status));
    }

    [Fact]
    public void APaidMonthCoversTheClubWhileItLasts()
    {
        var status = SubscriptionRules.StatusOf(Now.AddDays(-20), Now.AddDays(5), Now);

        Assert.Equal(SubscriptionStatus.Subscribed, status);
        Assert.False(SubscriptionRules.NeedsRegularization(status));
    }

    [Fact]
    public void TrialDaysLeftRoundUpAndNeverGoNegative()
    {
        Assert.Null(SubscriptionRules.TrialDaysLeft(null, Now));
        Assert.Equal(14, SubscriptionRules.TrialDaysLeft(Now.AddDays(14), Now));
        Assert.Equal(1, SubscriptionRules.TrialDaysLeft(Now.AddHours(1), Now));
        Assert.Equal(0, SubscriptionRules.TrialDaysLeft(Now.AddDays(-3), Now));
    }

    [Fact]
    public void AnExtensionAddsToWhatIsLeftOrCountsFromTodayWhenTheTrialAlreadyEnded()
    {
        Assert.Equal(Now.AddDays(10), SubscriptionRules.ExtendTrial(Now.AddDays(3), 7, Now));
        Assert.Equal(Now.AddDays(7), SubscriptionRules.ExtendTrial(Now.AddDays(-3), 7, Now));
    }

    [Fact]
    public void APaymentCoversThirtyDaysFromTodayOrFromTheEndOfThePaidMonth()
    {
        Assert.Equal(Now.AddDays(30), SubscriptionRules.MarkPaid(null, Now));
        Assert.Equal(Now.AddDays(30), SubscriptionRules.MarkPaid(Now.AddDays(-2), Now));
        Assert.Equal(Now.AddDays(40), SubscriptionRules.MarkPaid(Now.AddDays(10), Now));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(365, true)]
    [InlineData(366, false)]
    public void AnExtensionIsBetweenOneDayAndAYear(int days, bool isValid)
    {
        Assert.Equal(isValid, SubscriptionRules.IsValidExtension(days));
    }
}
