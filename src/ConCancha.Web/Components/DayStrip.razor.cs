using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class DayStrip
{
    private const int DefaultVisibleDays = 7;
    private const int TomorrowOffset = 1;
    private const int WeekdayAbbreviationLength = 3;

    [Parameter] public DateOnly Date { get; set; }
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public int DayCount { get; set; } = DefaultVisibleDays;
    [Parameter] public EventCallback<DateOnly> DateChanged { get; set; }

    private IEnumerable<DateOnly> Visible => Enumerable.Range(0, DayCount).Select(offset => Today.AddDays(offset));

    private string Label(DateOnly day)
    {
        if (day == Today) return "Hoy";
        return day == Today.AddDays(TomorrowOffset) ? "Mañana" : DisplayFormat.ShortDate(day)[..WeekdayAbbreviationLength];
    }
}
