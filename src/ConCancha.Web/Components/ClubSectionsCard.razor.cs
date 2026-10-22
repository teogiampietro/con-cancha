using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class ClubSectionsCard : CancellableComponentBase
{
    private static readonly IReadOnlyList<(ClubModules Module, string Label)> Sections =
    [
        (ClubModules.Buffet, "Buffet"),
        (ClubModules.Tournaments, "Torneos")
    ];

    private ClubModules _enabled;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isSaved;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public ClubDto Club { get; set; } = null!;
    [Parameter] public EventCallback<ClubDto> OnSaved { get; set; }

    protected override void OnInitialized() => _enabled = Club.EnabledModules;

    private bool IsOn(ClubModules section) => _enabled.HasFlag(section);

    private void Toggle(ClubModules section, bool isOn)
    {
        _isSaved = false;
        _enabled = isOn ? _enabled | section : _enabled & ~section;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        _isSaved = false;
        _isBusy = true;
        var result = await Api.UpdateClubModulesAsync(new UpdateClubModulesRequest(_enabled), cancellationToken);
        _isBusy = false;

        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        _isSaved = result.IsSuccess;
        if (result.IsSuccess) await OnSaved.InvokeAsync(result.Value);
    }
}
