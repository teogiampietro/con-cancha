using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class DateNavigator
{
    [Parameter] public DateOnly Date { get; set; }
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public EventCallback<DateOnly> DateChanged { get; set; }

    private Task MoveAsync(int days) => DateChanged.InvokeAsync(Date.AddDays(days));

    private Task GoToTodayAsync() => DateChanged.InvokeAsync(Today);
}
