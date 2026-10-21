using ConCancha.Contracts.Public;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class PublicBookingManage : CancellableComponentBase
{
    private PublicBookingDto? _booking;
    private string? _errorMessage;
    private bool _isConfirmingCancel;
    private bool _isBusy;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter] public string Slug { get; set; } = string.Empty;
    [Parameter] public string Token { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var result = await Api.GetPublicBookingAsync(Slug, Token, ComponentToken);
        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) _booking = result.Value;
    }

    private async Task CancelAsync(CancellationToken cancellationToken)
    {
        _isBusy = true;
        var result = await Api.CancelPublicBookingAsync(Slug, Token, cancellationToken);
        _isBusy = false;

        _isConfirmingCancel = false;
        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) _booking = result.Value;
    }
}
