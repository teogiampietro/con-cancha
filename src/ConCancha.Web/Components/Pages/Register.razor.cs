using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class Register : CancellableComponentBase
{
    private const int SlugTypingPauseMilliseconds = 400;
    private const string SlugAvailableMessage = "✓ Disponible";
    private const string ClubNameRequiredMessage = "Poné el nombre del club.";

    private bool _isLoading = true;
    private bool _isPasswordVisible;
    private bool _isBusy;
    private bool _isSlugEditable;
    private RegistrationStep _step = RegistrationStep.Account;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _name = string.Empty;
    private string _slug = string.Empty;
    private string _phone = string.Empty;
    private string? _errorMessage;
    private string? _slugMessage;
    private bool? _isSlugAvailable;
    private CancellationTokenSource? _slugCheck;

    private enum RegistrationStep
    {
        Account = 1,
        ClubName = 2,
        Phone = 3,
    }

    [Inject] private ApiClient Api { get; set; } = null!;
    [Inject] private CurrentSession Session { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await Session.LoadAsync(ComponentToken);
        if (Session.HasClub)
        {
            Navigation.NavigateTo(PageRoutes.Dashboard, replace: true);
            return;
        }

        // Account created but no club yet: resume at the club name step.
        if (Session.Current is not null) _step = RegistrationStep.ClubName;
        _isLoading = false;
        StateHasChanged();
    }

    protected override void OnDisposing() => _slugCheck?.Cancel();

    private void TogglePasswordVisibility() => _isPasswordVisible = !_isPasswordVisible;

    private void EnableSlugEditing() => _isSlugEditable = true;

    private void GoBackToClubName() => _step = RegistrationStep.ClubName;

    private async Task SubmitAccountAsync(CancellationToken cancellationToken)
    {
        _errorMessage = null;
        _isBusy = true;
        var result = await Api.RegisterAsync(_email, _password, cancellationToken);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            _errorMessage = result.GeneralMessage;
            return;
        }

        await Session.SaveAsync(result.Value!, cancellationToken);
        _password = string.Empty;
        _step = RegistrationStep.ClubName;
    }

    private async Task OnNameInputAsync(ChangeEventArgs args, CancellationToken cancellationToken)
    {
        _name = args.Value?.ToString() ?? string.Empty;
        if (!_isSlugEditable) _slug = SlugRules.Normalize(_name);
        await CheckSlugAvailabilityAsync(cancellationToken);
    }

    private async Task OnSlugInputAsync(ChangeEventArgs args, CancellationToken cancellationToken)
    {
        _slug = (args.Value?.ToString() ?? string.Empty).ToLowerInvariant();
        await CheckSlugAvailabilityAsync(cancellationToken);
    }

    /// <summary>Live availability: waits for a typing pause before asking the API.</summary>
    private async Task CheckSlugAvailabilityAsync(CancellationToken cancellationToken)
    {
        var checkToken = RestartSlugCheck(cancellationToken);
        _isSlugAvailable = null;
        _slugMessage = null;
        if (_slug.Length == 0) return;

        if (SlugRules.Validate(_slug) is { } reason)
        {
            SetSlugStatus(isAvailable: false, reason);
            return;
        }

        var result = await QuerySlugAfterPauseAsync(checkToken);
        if (result is null || checkToken.IsCancellationRequested) return;

        SetSlugStatus(result.IsSuccess && result.Value!.IsAvailable, result.Value?.Reason ?? result.GeneralMessage);
        StateHasChanged();
    }

    private async Task<ApiResult<SlugAvailabilityResponse>?> QuerySlugAfterPauseAsync(CancellationToken checkToken)
    {
        try
        {
            await Task.Delay(SlugTypingPauseMilliseconds, checkToken);
            return await Api.GetSlugAvailabilityAsync(_slug, checkToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private CancellationToken RestartSlugCheck(CancellationToken cancellationToken)
    {
        _slugCheck?.Cancel();
        _slugCheck = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return _slugCheck.Token;
    }

    private void SetSlugStatus(bool isAvailable, string? message)
    {
        _isSlugAvailable = isAvailable;
        _slugMessage = isAvailable ? SlugAvailableMessage : message;
    }

    private async Task SubmitClubNameAsync(CancellationToken cancellationToken)
    {
        _errorMessage = null;
        if (_name.Trim().Length < ClubLimits.NameMinLength)
        {
            _errorMessage = ClubNameRequiredMessage;
            return;
        }

        if (_slug.Length == 0) _slug = SlugRules.Normalize(_name);
        _errorMessage = await FindSlugProblemAsync(cancellationToken);
        if (_errorMessage is null) _step = RegistrationStep.Phone;
    }

    private async Task<string?> FindSlugProblemAsync(CancellationToken cancellationToken)
    {
        if (SlugRules.Validate(_slug) is { } reason) return reason;

        var result = await Api.GetSlugAvailabilityAsync(_slug, cancellationToken);
        if (!result.IsSuccess) return result.GeneralMessage;
        if (result.Value!.IsAvailable) return null;

        SetSlugStatus(isAvailable: false, result.Value.Reason);
        return result.Value.Reason;
    }

    private async Task SubmitClubAsync(CancellationToken cancellationToken)
    {
        _errorMessage = null;
        _isBusy = true;
        var result = await Api.CreateClubAsync(_name.Trim(), _slug, _phone, cancellationToken);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            HandleClubCreationFailure(result);
            return;
        }

        await Session.SaveAsync(result.Value!, cancellationToken);
        Navigation.NavigateTo(PageRoutes.Dashboard);
    }

    private void HandleClubCreationFailure(ApiResult<SessionResponse> result)
    {
        _errorMessage = result.GetFirstFieldError(ApiFieldNames.Slug) ?? result.GeneralMessage;

        // The problem is in the link: go back to that step.
        if (result.StatusCode is StatusCodes.Status400BadRequest or StatusCodes.Status409Conflict)
            _step = RegistrationStep.ClubName;
    }
}
