namespace ConCancha.Domain.Clubs;

public static class TeamPolicy
{
    public const int MaxStaffPerClub = 3;
    public static readonly TimeSpan InvitationValidity = TimeSpan.FromDays(7);

    public static bool HasRoomForAnother(int staffAndPendingInvitations) => staffAndPendingInvitations < MaxStaffPerClub;

    public static DateTimeOffset ExpiryFrom(DateTimeOffset now) => now + InvitationValidity;
}
