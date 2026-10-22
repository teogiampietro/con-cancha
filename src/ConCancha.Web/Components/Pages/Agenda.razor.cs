using ConCancha.Contracts.Agenda;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Courts;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Common;
using ConCancha.Domain.Courts;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ConCancha.Web.Components.Pages;

public partial class Agenda : PanelPageBase
{
    private const int PixelsPerHour = 72;
    private const int GridPaddingPixels = 10;
    private const int CardInsetPixels = 2;
    private const string GridId = "agenda-grid";
    private const string ScrollToNowFunction = "concancha.scrollToNow";
    private static readonly TimeSpan NowRefreshInterval = TimeSpan.FromMinutes(1);

    [Inject] private ClubClock Clock { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private sealed record SelectedFreeSlot(CourtDto Court, AgendaSlotDto Slot);

    private sealed record SelectedBooking(CourtDto Court, BookingDto Booking);

    private DateOnly _today;
    private DateOnly _date;
    private AgendaDayDto? _agenda;
    private SelectedFreeSlot? _selectedFreeSlot;
    private SelectedBooking? _selectedBooking;
    private IReadOnlyList<AgendaCourtDto> _visibleCourts = [];
    private IReadOnlyList<Sport> _sports = [];
    private Sport? _sportFilter;
    private int _gridStartMinute;
    private int _gridEndMinute;
    private int _nowMinute;
    private bool _scrollToNowPending;

    private bool ShowNowLine => _date == _today && _nowMinute >= _gridStartMinute && _nowMinute <= _gridEndMinute;

    private int ColumnHeight => OffsetOf(_gridEndMinute) + GridPaddingPixels;

    private IEnumerable<int> AxisHours =>
        Enumerable.Range(0, (_gridEndMinute - _gridStartMinute) / TimeConstants.MinutesPerHour + 1)
            .Select(step => _gridStartMinute + step * TimeConstants.MinutesPerHour);

    protected override async Task OnInitializedAsync()
    {
        if (!await EnterPanelAsync(ComponentToken)) return;

        _today = await Clock.GetTodayAsync(ComponentToken);
        _date = _today;
        _nowMinute = await Clock.GetMinuteOfDayAsync(ComponentToken);
        _scrollToNowPending = true;
        await LoadDayAsync(ComponentToken);
        _ = KeepNowCurrentAsync(ComponentToken);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_scrollToNowPending || _agenda is not { Courts.Count: > 0 }) return;

        _scrollToNowPending = false;
        if (ShowNowLine) await Js.InvokeVoidAsync(ScrollToNowFunction, ComponentToken, GridId);
    }

    private async Task KeepNowCurrentAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(NowRefreshInterval);
        try
        {
            do
            {
                _nowMinute = await Clock.GetMinuteOfDayAsync(cancellationToken);
                await InvokeAsync(StateHasChanged);
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadDayAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetAgendaAsync(_date, cancellationToken);
        if (!await SucceededAsync(result, cancellationToken)) return;

        ErrorText = null;
        _agenda = result.Value;
        ApplySportFilter();
    }

    private void FilterBySport(Sport? sport)
    {
        _sportFilter = sport;
        ApplySportFilter();
    }

    private void ApplySportFilter()
    {
        var courts = _agenda?.Courts ?? [];
        _sports = courts.Select(courtDay => courtDay.Court.Sport).Distinct().Order().ToList();
        if (_sportFilter is { } sport && !_sports.Contains(sport)) _sportFilter = null;

        _visibleCourts = courts.Where(courtDay => _sportFilter is null || courtDay.Court.Sport == _sportFilter).ToList();
        UpdateGridBounds();
    }

    private void UpdateGridBounds()
    {
        var slots = _visibleCourts.SelectMany(courtDay => courtDay.Slots).ToList();
        if (slots.Count == 0) return;

        _gridStartMinute = slots.Min(slot => slot.StartMinuteOfDay) / TimeConstants.MinutesPerHour * TimeConstants.MinutesPerHour;
        var lastEnd = slots.Max(slot => slot.EndMinuteOfDay);
        _gridEndMinute = (lastEnd + TimeConstants.MinutesPerHour - 1) / TimeConstants.MinutesPerHour * TimeConstants.MinutesPerHour;
    }

    private static int ToPixels(int minutes) => minutes * PixelsPerHour / TimeConstants.MinutesPerHour;

    private int OffsetOf(int minuteOfDay) => GridPaddingPixels + ToPixels(minuteOfDay - _gridStartMinute);

    private int CardTop(AgendaSlotDto slot) => OffsetOf(slot.StartMinuteOfDay) + CardInsetPixels;

    private int CardHeight(AgendaSlotDto slot) =>
        ToPixels(slot.EndMinuteOfDay - slot.StartMinuteOfDay) - CardInsetPixels * 2;

    private Task ChangeDayAsync(DateOnly date)
    {
        _date = date;
        _scrollToNowPending = date == _today;
        return LoadDayAsync(ComponentToken);
    }

    private void OpenSlot(AgendaCourtDto courtDay, AgendaSlotDto slot)
    {
        if (slot.Booking is { } booking)
            _selectedBooking = new SelectedBooking(courtDay.Court, booking);
        else
            _selectedFreeSlot = new SelectedFreeSlot(courtDay.Court, slot);
    }

    private void CloseSheet()
    {
        _selectedFreeSlot = null;
        _selectedBooking = null;
    }

    private async Task OnBookingChangedAsync()
    {
        CloseSheet();
        await LoadDayAsync(ComponentToken);
    }

    private static bool IsDisabled(AgendaSlotDto slot) => slot.IsPast && slot.Booking is null;

    private static string CardClass(AgendaSlotDto slot)
    {
        if (slot.Booking is not { } booking) return slot.IsPast ? "past" : "free";
        if (booking.Kind == BookingKind.Block) return "blk";
        return booking.PaymentStatus == PaymentStatus.Pending ? "bkc pend" : "bkc";
    }
}
