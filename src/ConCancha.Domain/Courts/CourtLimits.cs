namespace ConCancha.Domain.Courts;

public static class CourtLimits
{
    public const int NameMinLength = 1;
    public const int NameMaxLength = 40;
    public const int MaxCourtsPerSport = 20;

    public static readonly IReadOnlyList<int> AllowedSlotMinutes = [60, 90, 120];
}
