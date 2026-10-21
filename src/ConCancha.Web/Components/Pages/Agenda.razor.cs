using ConCancha.Contracts.Agenda;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Courts;
using ConCancha.Domain.Bookings;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class Agenda : PanelPageBase
{
    [Inject] private ClubClock Clock { get; set; } = null!;

    private sealed record SelectedFreeSlot(CourtDto Court, AgendaSlotDto Slot);

    private sealed record SelectedBooking(CourtDto Court, BookingDto Booking);

    private DateOnly _today;
    private DateOnly _date;
    private AgendaDayDto? _agenda;
    private SelectedFreeSlot? _selectedFreeSlot;
    private SelectedBooking? _selectedBooking;

    protected override async Task OnInitializedAsync()
    {
        if (!await EnterPanelAsync(ComponentToken)) return;

        _today = await Clock.GetTodayAsync(ComponentToken);
        _date = _today;
        await LoadDayAsync(ComponentToken);
    }

    private async Task LoadDayAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetAgendaAsync(_date, cancellationToken);
        if (!await SucceededAsync(result, cancellationToken)) return;

        ErrorText = null;
        _agenda = result.Value;
    }

    private Task ChangeDayAsync(DateOnly date)
    {
        _date = date;
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

    private static string SlotClass(AgendaSlotDto slot)
    {
        if (slot.Booking is not { } booking) return slot.IsPast ? "past" : "libre";
        if (booking.Kind == BookingKind.Block) return "block";
        return booking.PaymentStatus == PaymentStatus.Pending ? "busy pend" : "busy";
    }

    private static string BookingTitle(BookingDto booking) =>
        booking.Kind == BookingKind.Block ? $"Bloqueado: {booking.Reason}" : booking.CustomerName ?? string.Empty;
}
