using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class Team : PanelPageBase
{
    private const string AddedNotice = "Listo, ya es parte de tu equipo. Le avisamos por mail.";
    private const string InvitedNotice = "Le mandamos un mail con el link para crear su contraseña.";

    private IReadOnlyList<ClubMemberDto>? _team;
    private string _email = string.Empty;
    private string? _emailError;
    private string? _errorMessage;
    private string? _notice;
    private Guid? _confirmingId;
    private bool _isBusy;

    [Inject] private TimeProvider Clock { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (await EnterPanelAsync(ComponentToken, ownerOnly: true)) await ReloadAsync(ComponentToken);
    }

    private static bool IsInvited(ClubMemberDto member) => member.Status == TeamMemberStatus.Invited;

    private static bool IsOwnerEntry(ClubMemberDto member) => member.Role == nameof(ClubRole.Owner);

    private string Describe(ClubMemberDto member) => member switch
    {
        _ when IsOwnerEntry(member) => "Dueño",
        _ when IsInvited(member) => $"Encargado · invitado, {DescribeExpiry(member.ExpiresAt)}",
        _ => "Encargado · activo",
    };

    private string DescribeExpiry(DateTimeOffset? expiresAt)
    {
        var now = Clock.GetUtcNow();
        var days = (int)Math.Ceiling(((expiresAt ?? now) - now).TotalDays);
        return days <= 1 ? "el link vence hoy" : $"el link vence en {days} días";
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetCurrentClubTeamAsync(cancellationToken);
        if (await SucceededAsync(result, cancellationToken)) _team = result.Value;
    }

    private async Task InviteAsync(CancellationToken cancellationToken)
    {
        ClearMessages();
        _isBusy = true;
        var result = await Api.InviteTeamMemberAsync(_email.Trim(), cancellationToken);
        _isBusy = false;

        if (!result.IsSuccess) ShowErrors(result);
        else await OnInvitedAsync(result.Value!, cancellationToken);
    }

    private async Task OnInvitedAsync(ClubMemberDto member, CancellationToken cancellationToken)
    {
        _email = string.Empty;
        _notice = IsInvited(member) ? InvitedNotice : AddedNotice;
        await ReloadAsync(cancellationToken);
    }

    private void ShowErrors(ApiResult<ClubMemberDto> result)
    {
        _emailError = result.GetFirstFieldError(ApiFieldNames.Email);
        _errorMessage = _emailError is null ? result.GeneralMessage : null;
    }

    private async Task RemoveAsync(ClubMemberDto member, CancellationToken cancellationToken)
    {
        ClearMessages();
        _isBusy = true;
        var result = IsInvited(member)
            ? await Api.RevokeTeamInvitationAsync(member.Id, cancellationToken)
            : await Api.RemoveTeamMemberAsync(member.Id, cancellationToken);
        _isBusy = false;
        _confirmingId = null;

        if (result.IsSuccess) await ReloadAsync(cancellationToken);
        else _errorMessage = result.GeneralMessage;
    }

    private void AskConfirmation(ClubMemberDto member) => _confirmingId = member.Id;

    private void CancelConfirmation() => _confirmingId = null;

    private void ClearMessages() => _emailError = _errorMessage = _notice = null;
}
