using ConCancha.Contracts.Pricing;
using ConCancha.Domain.Common;
using ConCancha.Domain.Pricing;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class PriceRulesEditor : CancellableComponentBase
{
    private readonly HashSet<Weekdays> _days = [];

    private List<PriceRuleDto>? _rules;
    private Guid? _editingId;
    private bool _isFormOpen;
    private string _from = DisplayFormat.Time(0);
    private string _to = DisplayFormat.Time(TimeConstants.MinutesPerDay);
    private decimal? _price;
    private string? _errorMessage;
    private bool _isBusy;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public Guid CourtId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var result = await Api.GetPriceRulesAsync(CourtId, ComponentToken);
        if (result.IsSuccess) _rules = result.Value!.ToList();
        else _errorMessage = result.GeneralMessage;
    }

    private void ToggleDay(Weekdays day)
    {
        if (!_days.Remove(day)) _days.Add(day);
    }

    private void StartCreating() => OpenForm(null, Array.Empty<Weekdays>(), 0, TimeConstants.MinutesPerDay, null);

    private void StartEditing(PriceRuleDto rule) =>
        OpenForm(rule.Id, WeekdayList(rule.Days), rule.FromMinuteOfDay, rule.ToMinuteOfDay, DisplayFormat.Normalize(rule.Price));

    private void OpenForm(Guid? id, IEnumerable<Weekdays> days, int from, int to, decimal? price)
    {
        _editingId = id;
        _days.Clear();
        _days.UnionWith(days);
        _from = DisplayFormat.Time(from);
        _to = DisplayFormat.Time(to);
        _price = price;
        _errorMessage = null;
        _isFormOpen = true;
    }

    private void CloseForm()
    {
        _isFormOpen = false;
        _errorMessage = null;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!TryBuildRequest(out var request)) return;

        _isBusy = true;
        var result = _editingId is { } id
            ? await Api.UpdatePriceRuleAsync(id, request, cancellationToken)
            : await Api.CreatePriceRuleAsync(CourtId, request, cancellationToken);
        _isBusy = false;

        if (!result.IsSuccess) _errorMessage = result.GeneralMessage;
        else await ReloadAsync(cancellationToken);
    }

    private async Task DeleteAsync(PriceRuleDto rule, CancellationToken cancellationToken)
    {
        var result = await Api.DeletePriceRuleAsync(rule.Id, cancellationToken);
        if (!result.IsSuccess) _errorMessage = result.GeneralMessage;
        else await ReloadAsync(cancellationToken);
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetPriceRulesAsync(CourtId, cancellationToken);
        if (result.IsSuccess) _rules = result.Value!.ToList();
        CloseForm();
    }

    private bool TryBuildRequest(out PriceRuleRequest request)
    {
        request = new PriceRuleRequest(null, 0, 0, 0);
        var isTimeValid = DisplayFormat.TryParseTimeInput(_from, isEndOfDay: false, out var from)
            & DisplayFormat.TryParseTimeInput(_to, isEndOfDay: true, out var to);
        if (!isTimeValid || _price is not { } price)
        {
            _errorMessage = "Completá el horario y el precio.";
            return false;
        }

        request = new PriceRuleRequest(_days.Count == 0 ? null : _days.Aggregate(Weekdays.None, (all, day) => all | day), from, to, price);
        return true;
    }

    private static IEnumerable<Weekdays> WeekdayList(Weekdays? days) =>
        days is null ? [] : DisplayFormat.WeekdayLabels.Select(entry => entry.Day).Where(day => days.Value.HasFlag(day));
}
