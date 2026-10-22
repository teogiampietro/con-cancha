using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Layout;

public partial class PanelLayout : LayoutComponentBase
{
    private readonly CancellationTokenSource _disposal = new();
    private bool _isMoreOpen;

    [Inject] private CurrentSession Session { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Session.Changed += HandleSessionChanged;
        await Session.LoadAsync(_disposal.Token);
    }

    protected override void OnParametersSet() => _isMoreOpen = false;

    public void Dispose()
    {
        Session.Changed -= HandleSessionChanged;
        _disposal.Cancel();
        _disposal.Dispose();
    }

    private void HandleSessionChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task SignOutAsync()
    {
        await Session.ClearAsync(_disposal.Token);
        Navigation.NavigateTo(PageRoutes.Login);
    }
}
