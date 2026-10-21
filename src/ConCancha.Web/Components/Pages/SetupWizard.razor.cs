using ConCancha.Domain.Courts;
using ConCancha.Web.Services;
using ConCancha.Web.Services.Setup;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class SetupWizard : PanelPageBase
{
    private const int PercentScale = 100;

    private static readonly IReadOnlyList<Sport> AllSports = Enum.GetValues<Sport>();

    private SetupDraft _draft = new();
    private int _stepIndex;
    private bool _isLoaded;
    private bool _isBusy;
    private bool _isEditingFromSummary;

    private IReadOnlyList<SetupStep> Steps => _draft.BuildSteps();

    private SetupStep CurrentStep => Steps[Math.Min(_stepIndex, Steps.Count - 1)];

    private bool IsLastStep => _stepIndex >= Steps.Count - 1;

    private int ProgressPercent => (_stepIndex + 1) * PercentScale / Steps.Count;

    private string NextLabel => _isBusy ? "Guardando…" : IsLastStep ? "Crear mi club" : _isEditingFromSummary ? "Volver al resumen" : "Siguiente";

    protected override async Task OnInitializedAsync()
    {
        if (!await EnterPanelAsync(ComponentToken, ownerOnly: true)) return;

        var result = await Api.GetSetupProgressAsync(ComponentToken);
        if (!await SucceededAsync(result, ComponentToken)) return;
        if (result.Value!.IsCompleted)
        {
            Navigation.NavigateTo(PageRoutes.Agenda, replace: true);
            return;
        }

        if (result.Value.Draft is { } saved) _draft = SetupDraft.From(saved);
        _stepIndex = Math.Min(result.Value.Step, Steps.Count - 1);
        _isLoaded = true;
    }

    private void ToggleSport(Sport sport) => _draft.ToggleSport(sport);

    private void SetOpening(ChangeEventArgs args) => _draft.OpeningText = args.Value?.ToString() ?? string.Empty;

    private void SetClosing(ChangeEventArgs args) => _draft.ClosingText = args.Value?.ToString() ?? string.Empty;

    private void SetSlotMinutes(int minutes) => CurrentStep.Sport!.SlotMinutes = minutes;

    private async Task NextAsync(CancellationToken cancellationToken)
    {
        ErrorText = _draft.Validate(CurrentStep);
        if (ErrorText is not null || _isBusy) return;

        if (IsLastStep)
            await CompleteAsync(cancellationToken);
        else
            await MoveToAsync(_isEditingFromSummary ? Steps.Count - 1 : _stepIndex + 1, cancellationToken);
    }

    private Task BackAsync(CancellationToken cancellationToken)
    {
        ErrorText = null;
        return MoveToAsync(_stepIndex - 1, cancellationToken);
    }

    private async Task EditAsync(SetupStepKind kind, CancellationToken cancellationToken)
    {
        _isEditingFromSummary = true;
        await MoveToAsync(Steps.ToList().FindIndex(step => step.Kind == kind), cancellationToken);
    }

    private async Task MoveToAsync(int stepIndex, CancellationToken cancellationToken)
    {
        _stepIndex = Math.Clamp(stepIndex, 0, Steps.Count - 1);
        if (IsLastStep) _isEditingFromSummary = false;

        var result = await Api.SaveSetupProgressAsync(_stepIndex, _draft.ToDto(), cancellationToken);
        await SucceededAsync(result, cancellationToken);
    }

    private async Task CompleteAsync(CancellationToken cancellationToken)
    {
        _isBusy = true;
        var result = await Api.CompleteSetupAsync(_draft.ToDto(), cancellationToken);
        _isBusy = false;
        if (!await SucceededAsync(result, cancellationToken)) return;

        Navigation.NavigateTo(PageRoutes.Agenda);
    }
}
