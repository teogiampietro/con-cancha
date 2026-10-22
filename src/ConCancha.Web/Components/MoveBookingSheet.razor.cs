using ConCancha.Contracts.Agenda;
using ConCancha.Contracts.Bookings;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class MoveBookingSheet : CancellableComponentBase
{
    private sealed record MoveOption(Guid CourtId, string CourtName, int StartMinuteOfDay)
    {
        public string Key => $"{CourtId}|{StartMinuteOfDay}";
    }

    private IReadOnlyList<MoveOption>? _options;
    private string _selectedKey = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public string CourtName { get; set; } = string.Empty;
    [Parameter, EditorRequired] public BookingDto Booking { get; set; } = null!;
    [Parameter] public EventCallback OnMoved { get; set; }
    [Parameter] public EventCallback OnBack { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var result = await Api.GetAgendaAsync(Booking.Date, ComponentToken);
        if (!result.IsSuccess)
        {
            _errorMessage = result.GeneralMessage;
            return;
        }

        _options = FreeSlotsOfSameDuration(result.Value!);
        _selectedKey = _options.FirstOrDefault()?.Key ?? string.Empty;
    }

    private IReadOnlyList<MoveOption> FreeSlotsOfSameDuration(AgendaDayDto agenda)
    {
        var duration = Booking.EndMinuteOfDay - Booking.StartMinuteOfDay;
        return agenda.Courts
            .SelectMany(courtDay => courtDay.Slots
                .Where(slot => slot.Booking is null && !slot.IsPast && slot.EndMinuteOfDay - slot.StartMinuteOfDay == duration)
                .Select(slot => new MoveOption(courtDay.Court.Id, courtDay.Court.Name, slot.StartMinuteOfDay)))
            .OrderBy(option => option.StartMinuteOfDay).ThenBy(option => option.CourtName)
            .ToList();
    }

    private async Task MoveAsync(CancellationToken cancellationToken)
    {
        if (_options?.FirstOrDefault(option => option.Key == _selectedKey) is not { } target) return;

        _isBusy = true;
        var result = await Api.MoveBookingAsync(Booking.Id, new MoveBookingRequest(target.CourtId, target.StartMinuteOfDay), cancellationToken);
        _isBusy = false;

        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) await OnMoved.InvokeAsync();
    }
}
