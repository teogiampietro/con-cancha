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
    private const string CodeResentMessage = "Te mandamos un código nuevo.";

    private bool _isLoading = true;
    private bool _isPasswordVisible;
    private bool _isBusy;
    private bool _isSlugEditable;
    private RegistrationStep _step = RegistrationStep.Account;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _code = string.Empty;
    private string _name = string.Empty;
    private string _slug = string.Empty;
    private string _phone = string.Empty;
    private string _clubNameInitialValue = string.Empty;
    private string _slugInitialValue = string.Empty;
    private string? _errorMessage;
    private string? _slugMessage;
    private string? _infoMessage;
    private bool? _isSlugAvailable;
    private CancellationTokenSource? _slugCheck;

    private enum RegistrationStep
    {
        Account = 1,
        VerifyEmail = 2,
        ClubName = 3,
        Phone = 4,
    }

    [SupplyParameterFromQuery(Name = "mail")] private string? PendingEmail { get; set; }

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
        if (Session.Current is not null) ShowClubNameStep();
        else if (!string.IsNullOrWhiteSpace(PendingEmail)) ShowVerifyEmailStep(PendingEmail);
        _isLoading = false;
        StateHasChanged();
    }

    protected override void OnDisposing() => _slugCheck?.Cancel();

    private void TogglePasswordVisibility() => _isPasswordVisible = !_isPasswordVisible;

    private void EnableSlugEditing()
    {
        _slugInitialValue = _slug;
        _isSlugEditable = true;
    }

    private void GoBackToClubName() => ShowClubNameStep();

    private void GoBackToAccount()
    {
        _errorMessage = null;
        _infoMessage = null;
        _step = RegistrationStep.Account;
    }

    private void ShowVerifyEmailStep(string email)
    {
        _email = email;
        _code = string.Empty;
        _errorMessage = null;
        _infoMessage = null;
        _step = RegistrationStep.VerifyEmail;
    }

    /// <remarks>
    /// The inputs only take their value from the server when they are created. Typing never re-sends it: with the
    /// server on another continent, a re-render with an older value would overwrite the characters typed meanwhile.
    /// </remarks>
    private void ShowClubNameStep()
    {
        _clubNameInitialValue = _name;
        _slugInitialValue = _slug;
        _step = RegistrationStep.ClubName;
    }

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

        _password = string.Empty;
        ShowVerifyEmailStep(_email);
    }

    private async Task SubmitCodeAsync(CancellationToken cancellationToken)
    {
        _errorMessage = null;
        _infoMessage = null;
        _isBusy = true;
        var result = await Api.VerifyEmailAsync(_email, _code, cancellationToken);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            _errorMessage = result.GetFirstFieldError(ApiFieldNames.Code) ?? result.GeneralMessage;
            return;
        }

        await Session.SaveAsync(result.Value!, cancellationToken);
        ShowClubNameStep();
    }

    private async Task ResendCodeAsync(CancellationToken cancellationToken)
    {
        _errorMessage = null;
        _isBusy = true;
        var result = await Api.ResendVerificationAsync(_email, cancellationToken);
        _isBusy = false;

        if (result.IsSuccess) _infoMessage = CodeResentMessage;
        else _errorMessage = result.GeneralMessage;
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
            ShowClubNameStep();
    }
}
