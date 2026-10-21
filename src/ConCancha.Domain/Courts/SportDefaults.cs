namespace ConCancha.Domain.Courts;

public static class SportDefaults
{
    private const int NinetyMinutes = 90;
    private const int SixtyMinutes = 60;

    public static int GetSlotMinutes(Sport sport) => sport == Sport.Padel ? NinetyMinutes : SixtyMinutes;
}
