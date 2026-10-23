namespace ConCancha.Domain.Platform;

public static class ClubActivityRules
{
    public const int NewClubDays = 14;

    /// <param name="recentBookings">Turns of the current and the previous week.</param>
    public static ClubActivity Classify(DateOnly createdOn, DateOnly today, int recentBookings)
    {
        if (recentBookings > 0) return ClubActivity.Active;

        return today.DayNumber - createdOn.DayNumber < NewClubDays ? ClubActivity.New : ClubActivity.Inactive;
    }
}
