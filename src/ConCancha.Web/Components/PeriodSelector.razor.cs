using ConCancha.Domain.Platform;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

/// <summary>Picks the period of a platform page. The choice goes into the URL, so it survives reloads and moves between pages.</summary>
public partial class PeriodSelector : ComponentBase
{
    private static readonly IReadOnlyList<(PeriodShortcut Shortcut, string Label)> Options =
    [
        (PeriodShortcut.Today, "Hoy"),
        (PeriodShortcut.ThisWeek, "Esta semana"),
        (PeriodShortcut.LastWeek, "Semana pasada"),
        (PeriodShortcut.ThisMonth, "Este mes"),
        (PeriodShortcut.Last30Days, "Últimos 30 días"),
    ];

    private PlatformPeriod? _synced;
    private bool _isCustom;
    private string _from = string.Empty;
    private string _to = string.Empty;
    private string? _error;

    [Parameter, EditorRequired] public PlatformPeriod Period { get; set; } = null!;

    [Parameter, EditorRequired] public DateOnly Today { get; set; }

    /// <summary>The page the period applies to, such as <c>/plataforma</c>.</summary>
    [Parameter, EditorRequired] public string BasePath { get; set; } = string.Empty;

    [Inject] private NavigationManager Navigation { get; set; } = null!;

    protected override void OnParametersSet()
    {
        if (_synced == Period) return;

        _synced = Period;
        _from = PlatformPeriods.Format(Period.From);
        _to = PlatformPeriods.Format(Period.To);
        _isCustom = PlatformPeriods.ShortcutOf(Period, Today) is null;
        _error = null;
    }

    private bool IsActive(PeriodShortcut shortcut) => !_isCustom && PlatformPeriods.ShortcutOf(Period, Today) == shortcut;

    private void ChooseShortcut(PeriodShortcut shortcut)
    {
        _isCustom = false;
        GoTo(PlatformPeriods.Resolve(shortcut, Today));
    }

    private void ShowCustom()
    {
        _isCustom = true;
        _error = null;
    }

    private void ApplyCustom()
    {
        if (!PlatformPeriods.TryParseDay(_from, out var from) || !PlatformPeriods.TryParseDay(_to, out var to))
        {
            _error = PlatformPeriods.MissingDatesMessage;
            return;
        }

        if (UsagePeriod.Create(from, to) is null)
        {
            _error = PlatformPeriods.ValidationMessage(from, to);
            return;
        }

        GoTo(new PlatformPeriod(from, to));
    }

    private void GoTo(PlatformPeriod period) => Navigation.NavigateTo($"{BasePath}?{PlatformPeriods.Query(period)}");
}
