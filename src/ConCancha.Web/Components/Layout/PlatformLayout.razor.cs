using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Layout;

public partial class PlatformLayout : LayoutComponentBase
{
    private readonly CancellationTokenSource _disposal = new();
    private bool _isReady;

    [Inject] private PlatformSession Session { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await Session.LoadAsync(_disposal.Token);
        _isReady = Session.Current is not null;
        if (!_isReady) Navigation.NavigateTo(PageRoutes.PlatformLogin, replace: true);
    }

    public void Dispose()
    {
        _disposal.Cancel();
        _disposal.Dispose();
    }

    private async Task SignOutAsync()
    {
        await Session.ClearAsync(_disposal.Token);
        Navigation.NavigateTo(PageRoutes.PlatformLogin, replace: true);
    }
}
