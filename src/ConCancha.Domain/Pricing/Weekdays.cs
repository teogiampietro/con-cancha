namespace ConCancha.Domain.Pricing;

[Flags]
public enum Weekdays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
}

public static class WeekdaysExtensions
{
    public static Weekdays From(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekdays.Monday,
        DayOfWeek.Tuesday => Weekdays.Tuesday,
        DayOfWeek.Wednesday => Weekdays.Wednesday,
        DayOfWeek.Thursday => Weekdays.Thursday,
        DayOfWeek.Friday => Weekdays.Friday,
        DayOfWeek.Saturday => Weekdays.Saturday,
        _ => Weekdays.Sunday,
    };
}
