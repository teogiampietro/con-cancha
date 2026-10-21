using ConCancha.Contracts.Reminders;
using ConCancha.Web.Services;

namespace ConCancha.Web.Components.Pages;

public partial class Reminders : PanelPageBase
{
    private ReminderListDto? _list;
    private ReminderDay _day = ReminderDay.Tomorrow;

    protected override async Task OnInitializedAsync()
    {
        if (await EnterPanelAsync(ComponentToken)) await LoadAsync(ComponentToken);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetRemindersAsync(_day, cancellationToken);
        if (await SucceededAsync(result, cancellationToken)) _list = result.Value;
    }

    private Task ChangeDayAsync(ReminderDay day)
    {
        _day = day;
        return LoadAsync(ComponentToken);
    }

    private string LinkFor(ReminderDto reminder) =>
        ReminderMessage.WhatsAppUrl(reminder.WhatsAppPhone, ReminderMessage.Build(_list!.ClubName, reminder));

    private async Task MarkSentAsync(ReminderDto reminder)
    {
        var result = await Api.MarkReminderSentAsync(reminder.BookingId, ComponentToken);
        if (result.IsSuccess) await LoadAsync(ComponentToken);
    }
}
