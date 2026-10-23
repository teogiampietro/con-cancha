using ConCancha.Contracts.Courts;
using ConCancha.Contracts.RecurringBookings;
using ConCancha.Domain.Courts;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class RecurringGrid : CancellableComponentBase
{
    private sealed record Cell(DayOfWeek Day, int Start, RecurringBookingDto? Item);

    private CourtDto _court = null!;
    private IReadOnlyList<RecurringBookingDto>? _items;
    private IReadOnlyList<int> _starts = [];
    private Cell? _cell;
    private string? _notice;
    private string? _errorMessage;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public IReadOnlyList<CourtDto> Courts { get; set; } = [];
    [Parameter, EditorRequired] public int ClubOpeningMinuteOfDay { get; set; }
    [Parameter, EditorRequired] public int ClubClosingMinuteOfDay { get; set; }

    private IReadOnlyList<RecurringBookingDto> Paused => _items?.Where(item => !item.IsActive).ToList() ?? [];

    protected override async Task OnInitializedAsync()
    {
        if (Courts.Count == 0) return;

        await SelectCourtAsync(Courts[0]);
    }

    private async Task SelectCourtAsync(CourtDto court)
    {
        _court = court;
        _notice = null;
        _items = null;
        var hours = new OperatingHours(
            court.OpeningMinuteOfDay ?? ClubOpeningMinuteOfDay, court.ClosingMinuteOfDay ?? ClubClosingMinuteOfDay);
        _starts = SlotGenerator.GetStartMinutes(hours, court.SlotMinutes);
        await ReloadAsync(ComponentToken);
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetRecurringBookingsAsync(_court.Id, cancellationToken);
        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) _items = result.Value;
    }

    private RecurringBookingDto? FindActive(DayOfWeek day, int start) =>
        _items?.FirstOrDefault(item => item.IsActive && item.Day == day && item.StartMinuteOfDay == start);

    private void OpenCell(DayOfWeek day, int start, RecurringBookingDto? item) => _cell = new Cell(day, start, item);

    private void CloseCell() => _cell = null;

    private async Task OnSavedAsync(RecurringBookingResultDto result)
    {
        _cell = null;
        _notice = DescribeResult(result);
        await ReloadAsync(ComponentToken);
    }

    private async Task OnRemovedAsync()
    {
        _cell = null;
        _notice = "Fijo quitado. Se cancelaron sus turnos futuros.";
        await ReloadAsync(ComponentToken);
    }

    private async Task RemovePausedAsync(RecurringBookingDto item, CancellationToken cancellationToken)
    {
        var result = await Api.DeleteRecurringBookingAsync(_court.Id, item.Id, cancellationToken);
        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        await ReloadAsync(cancellationToken);
    }

    private static string DescribeResult(RecurringBookingResultDto result)
    {
        var saved = $"Fijo guardado: {result.CreatedCount} turnos agendados.";
        if (result.SkippedDates.Count == 0) return saved;

        var dates = string.Join(", ", result.SkippedDates.Select(DisplayFormat.ShortDate));
        return $"{saved} No pudimos agendar {dates} porque el horario estaba ocupado o sin precio.";
    }

    private static string FirstName(RecurringBookingDto item) => item.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    private static string CellLabel(DayOfWeek day, int start, RecurringBookingDto? item) =>
        item is null
            ? $"Sumar fijo {DisplayFormat.DayName(day)} {DisplayFormat.Time(start)}"
            : $"Fijo de {item.CustomerName} {DisplayFormat.DayName(day)} {DisplayFormat.Time(start)}";
}
