using ConCancha.Contracts.Clubs;
using ConCancha.Contracts.Courts;
using ConCancha.Web.Services;

namespace ConCancha.Web.Components.Pages;

public partial class Courts : PanelPageBase
{
    private enum CourtsTab
    {
        Courts,
        Recurring,
    }

    private CourtsTab _tab = CourtsTab.Courts;
    private ClubDto? _club;
    private IReadOnlyList<CourtDto>? _courts;
    private Guid? _editingCourtId;
    private Guid? _pricesCourtId;
    private bool _isCreating;

    private IReadOnlyList<CourtDto> ActiveCourts => _courts?.Where(court => court.IsActive).ToList() ?? [];

    protected override async Task OnInitializedAsync()
    {
        if (!await EnterPanelAsync(ComponentToken, ownerOnly: true)) return;

        var club = await Api.GetCurrentClubAsync(ComponentToken);
        if (!await SucceededAsync(club, ComponentToken)) return;

        _club = club.Value;
        await ReloadCourtsAsync(ComponentToken);
    }

    private async Task ReloadCourtsAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetCourtsAsync(cancellationToken);
        if (await SucceededAsync(result, cancellationToken)) _courts = result.Value;
    }

    private void OnHoursSaved(ClubDto club) => _club = club;

    private void StartCreating()
    {
        _editingCourtId = null;
        _isCreating = true;
    }

    private void EditCourt(CourtDto court)
    {
        _isCreating = false;
        _editingCourtId = court.Id;
    }

    private void TogglePrices(CourtDto court) => _pricesCourtId = _pricesCourtId == court.Id ? null : court.Id;

    private void CloseCourtForm()
    {
        _isCreating = false;
        _editingCourtId = null;
    }

    private async Task OnCourtSaved(CourtDto court)
    {
        var isNew = _isCreating;
        CloseCourtForm();
        await ReloadCourtsAsync(ComponentToken);
        if (isNew) _pricesCourtId = court.Id;
    }

    private string DescribeHours(CourtDto court) =>
        court.OpeningMinuteOfDay is { } opening && court.ClosingMinuteOfDay is { } closing
            ? $"horario propio de {DisplayFormat.Time(opening)} a {DisplayFormat.Time(closing)}"
            : "horario del club";
}
