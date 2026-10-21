using ConCancha.Contracts.Agenda;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Courts;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Web.Services;

namespace ConCancha.Web.Components.Pages;

public partial class Agenda : PanelPageBase
{
    private sealed record SelectedFreeSlot(CourtDto Court, AgendaSlotDto Slot);

    private sealed record SelectedBooking(CourtDto Court, BookingDto Booking);

    private TimeZoneInfo _clubZone = TimeZoneInfo.FindSystemTimeZoneById(ClubDefaults.TimeZoneId);
    private DateOnly _date;
    private AgendaDayDto? _agenda;
    private SelectedFreeSlot? _selectedFreeSlot;
    private SelectedBooking? _selectedBooking;

    private DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _clubZone).DateTime);

    protected override async Task OnInitializedAsync()
    {
        if (!await EnterPanelAsync(ComponentToken)) return;

        var club = await Api.GetCurrentClubAsync(ComponentToken);
        if (!await SucceededAsync(club, ComponentToken)) return;

        _clubZone = TimeZoneInfo.FindSystemTimeZoneById(club.Value!.TimeZoneId);
        _date = Today;
        await LoadDayAsync(ComponentToken);
    }

    private async Task LoadDayAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetAgendaAsync(_date, cancellationToken);
        if (!await SucceededAsync(result, cancellationToken)) return;

        ErrorText = null;
        _agenda = result.Value;
    }

    private Task ChangeDayAsync(int days)
    {
        _date = _date.AddDays(days);
        return LoadDayAsync(ComponentToken);
    }

    private Task GoToTodayAsync()
    {
        _date = Today;
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

    private static string BookingChipText(BookingDto booking)
    {
        if (booking.Kind == BookingKind.Block) return "Bloqueo";
        if (booking.Attendance == Attendance.NoShow) return "No vino";
        if (booking.Attendance == Attendance.Played) return "Jugado";
        return booking.PaymentStatus == PaymentStatus.Paid ? "Pagó" : "Debe";
    }

    private static string BookingChipClass(BookingDto booking)
    {
        if (booking.Kind == BookingKind.Block) return string.Empty;
        if (booking.Attendance == Attendance.NoShow) return "warn";
        return booking.Attendance == Attendance.Played || booking.PaymentStatus == PaymentStatus.Paid ? "ok" : "warn";
    }
}
