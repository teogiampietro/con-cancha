using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class Register : CancellableComponentBase
{
    private const int SlugTypingPauseMilliseconds = 1500;
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

    private void OnNameInput(ChangeEventArgs args)
    {
        _name = args.Value?.ToString() ?? string.Empty;
        if (!_isSlugEditable) _slug = SlugRules.Normalize(_name);
        ScheduleSlugCheck();
    }

    private void OnSlugInput(ChangeEventArgs args)
    {
        _slug = (args.Value?.ToString() ?? string.Empty).ToLowerInvariant();
        ScheduleSlugCheck();
    }

    /// <remarks>
    /// Not awaited on purpose: an input handler that waits makes Blazor re-render with the value of an old keystroke,
    /// which overwrites the characters typed meanwhile.
    /// </remarks>
    private void ScheduleSlugCheck()
    {
        _isSlugAvailable = null;
        _slugMessage = null;
        _ = CheckSlugAvailabilityAfterPauseAsync(RestartSlugCheck());
    }

    /// <summary>Live availability: runs once the person stops typing for <see cref="SlugTypingPauseMilliseconds"/>.</summary>
    private async Task CheckSlugAvailabilityAfterPauseAsync(CancellationToken checkToken)
    {
        try
        {
            await Task.Delay(SlugTypingPauseMilliseconds, checkToken);
            await CheckCurrentSlugAsync(checkToken);
        }
        catch (OperationCanceledException) when (checkToken.IsCancellationRequested)
        {
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task CheckCurrentSlugAsync(CancellationToken checkToken)
    {
        if (_slug.Length == 0) return;

        if (SlugRules.Validate(_slug) is { } reason)
        {
            SetSlugStatus(isAvailable: false, reason);
            return;
        }

        var result = await Api.GetSlugAvailabilityAsync(_slug, checkToken);
        SetSlugStatus(result.IsSuccess && result.Value!.IsAvailable, result.Value?.Reason ?? result.GeneralMessage);
    }

    private CancellationToken RestartSlugCheck()
    {
        _slugCheck?.Cancel();
        _slugCheck = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
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
