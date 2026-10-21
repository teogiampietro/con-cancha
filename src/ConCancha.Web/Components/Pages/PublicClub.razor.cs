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
    private string? _errorMessage;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter] public string Slug { get; set; } = string.Empty;

    private DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _clubZone).DateTime);

    private IEnumerable<DateOnly> Days => Enumerable.Range(0, BookingPolicy.PublicBookingDays).Select(offset => Today.AddDays(offset));

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
        await LoadDayAsync(ComponentToken);
    }

    private async Task LoadDayAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetPublicDayAsync(Slug, _date, cancellationToken);
        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) _day = result.Value;
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
        await LoadDayAsync(ComponentToken);
    }

    private string DayLabel(DateOnly day)
    {
        if (day == Today) return "Hoy";
        return day == Today.AddDays(1) ? "Mañana" : DisplayFormat.ShortDate(day)[..3];
    }

    private static string SlotClass(PublicSlotDto slot) => slot.State switch
    {
        PublicSlotState.Free => "libre",
        PublicSlotState.Past => "past",
        _ => "busy",
    };

    private static string SlotText(PublicSlotDto slot) => slot.State switch
    {
        PublicSlotState.Free => "Libre",
        PublicSlotState.Past => "Pasó",
        _ => "Ocupado",
    };
}
