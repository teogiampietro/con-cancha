using ConCancha.Contracts.Reservations;

namespace ConCancha.Web.Components.Pages;

public partial class Reservations : PanelPageBase
{
    private static readonly IReadOnlyList<(ReservationRange Range, string Label)> Ranges =
    [
        (ReservationRange.Today, "Hoy"), (ReservationRange.Next7Days, "7 días"),
        (ReservationRange.Next30Days, "30 días"), (ReservationRange.History, "Historial"),
    ];

    private static readonly IReadOnlyList<(ReservationFilter Filter, string Label)> Filters =
    [
        (ReservationFilter.All, "Todas"), (ReservationFilter.ToCollect, "A cobrar"), (ReservationFilter.Online, "Online"),
    ];

    private ReservationListDto? _list;
    private ReservationItemDto? _selected;
    private ReservationRange _range = ReservationRange.Today;
    private ReservationFilter _filter = ReservationFilter.All;
    private string _search = string.Empty;
    private int _loadVersion;

    protected override async Task OnInitializedAsync()
    {
        if (await EnterPanelAsync(ComponentToken)) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var version = ++_loadVersion;
        var result = await Api.GetReservationsAsync(_range, _filter, _search, ComponentToken);
        if (version != _loadVersion) return;

        if (await SucceededAsync(result, ComponentToken)) _list = result.Value;
    }

    private Task ChangeRangeAsync(ReservationRange range)
    {
        _range = range;
        return LoadAsync();
    }

    private Task ChangeFilterAsync(ReservationFilter filter)
    {
        _filter = filter;
        return LoadAsync();
    }

    private async Task OnChangedAsync()
    {
        _selected = null;
        await LoadAsync();
    }
}
