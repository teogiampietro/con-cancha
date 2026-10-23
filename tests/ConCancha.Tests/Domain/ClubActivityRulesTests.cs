using ConCancha.Domain.Platform;

namespace ConCancha.Tests.Domain;

public class ClubActivityRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 23);

    [Fact]
    public void AClubWithRecentTurnsIsActiveWhateverItsAge()
    {
        Assert.Equal(ClubActivity.Active, ClubActivityRules.Classify(Today.AddDays(-400), Today, recentBookings: 1));
    }

    [Fact]
    public void AClubWithoutTurnsIsNewUntilItTurnsFourteenDaysOld()
    {
        Assert.Equal(ClubActivity.New, ClubActivityRules.Classify(Today.AddDays(-13), Today, recentBookings: 0));
        Assert.Equal(ClubActivity.Inactive, ClubActivityRules.Classify(Today.AddDays(-14), Today, recentBookings: 0));
    }
}
