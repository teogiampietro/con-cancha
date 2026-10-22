using ConCancha.Contracts.Clubs;
using ConCancha.Contracts.Public;
using ConCancha.Domain.Bookings;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class PublicClub : CancellableComponentBase
{
    private TimeZoneInfo _clubZone = TimeZoneInfo.Utc;
    private PublicClubDto? _club;
    private PublicDayDto? _day;
    private DateOnly _date;
    private Guid? _selectedCourtId;
    private PublicSlotDto? _selectedSlot;
    private string? _notFoundMessage;
    private PublicMyBookingsDto? _myBookings;
    private string? _visitorToken;
    private string? _errorMessage;
    private bool _isCancelling;

    [Inject] private ApiClient Api { get; set; } = null!;
    [Inject] private VisitorTokenStore Visitor { get; set; } = null!;

    [Parameter] public string Slug { get; set; } = string.Empty;

    private string ThemeStyle => _club is null ? string.Empty : ClubTheme.Style(_club.PrimaryColor, _club.SecondaryColor);

    private DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _clubZone).DateTime);

    private PublicCourtDto? SelectedCourt =>
        _day?.Courts.FirstOrDefault(court => court.Id == _selectedCourtId) ?? _day?.Courts.FirstOrDefault();

    protected override async Task OnInitializedAsync()
    {
        var result = await Api.GetPublicClubAsync(Slug, ComponentToken);
        if (!result.IsSuccess)
        {
            _notFoundMessage = result.StatusCode == StatusCodes.Status404NotFound ? "No encontramos ese club." : result.GeneralMessage;
            return;
        }

        _club = result.Value;
        _clubZone = TimeZoneInfo.FindSystemTimeZoneById(_club!.TimeZoneId);
        _date = Today;
        _visitorToken = await Visitor.GetAsync(ComponentToken);
        await Task.WhenAll(LoadDayAsync(ComponentToken), LoadMyBookingsAsync(ComponentToken));
    }

    private async Task LoadDayAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetPublicDayAsync(Slug, _date, _visitorToken, cancellationToken);
        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) _day = result.Value;
    }

    private async Task LoadMyBookingsAsync(CancellationToken cancellationToken)
    {
        if (_visitorToken is null) return;

        var result = await Api.GetMyPublicBookingsAsync(Slug, _visitorToken, cancellationToken);
        if (result.IsSuccess) _myBookings = result.Value;
    }

    private async Task CancelMineAsync(Guid bookingId)
    {
        _isCancelling = true;
        var result = await Api.CancelMyPublicBookingAsync(Slug, bookingId, _visitorToken, ComponentToken);
        _isCancelling = false;

        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) _myBookings = result.Value;
        await LoadDayAsync(ComponentToken);
    }

    private Task SelectDayAsync(DateOnly day)
    {
        _date = day;
        return LoadDayAsync(ComponentToken);
    }

    private void OpenSheet(PublicSlotDto slot) => _selectedSlot = slot;

    private async Task CloseSheetAsync()
    {
        _selectedSlot = null;
        _visitorToken = await Visitor.GetAsync(ComponentToken);
        await Task.WhenAll(LoadDayAsync(ComponentToken), LoadMyBookingsAsync(ComponentToken));
    }

    private static string SlotClass(PublicSlotDto slot) => slot.State switch
    {
        PublicSlotState.Free => "libre",
        PublicSlotState.Past => "past",
        PublicSlotState.Mine => "mine",
        PublicSlotState.Blocked => "block",
        _ => "busy",
    };

    private static string SlotText(PublicSlotDto slot) => slot.State switch
    {
        PublicSlotState.Free => "Libre",
        PublicSlotState.Past => "Pasó",
        PublicSlotState.Mine => "Tu turno",
        PublicSlotState.Blocked => "No disponible",
        _ => "Ocupado",
    };
}
