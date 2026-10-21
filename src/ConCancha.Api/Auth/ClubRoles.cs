using ConCancha.Domain.Clubs;

namespace ConCancha.Api.Auth;

public static class ClubRoles
{
    public const string Owner = nameof(ClubRole.Owner);
    public const string Any = nameof(ClubRole.Owner) + "," + nameof(ClubRole.Staff);
}
