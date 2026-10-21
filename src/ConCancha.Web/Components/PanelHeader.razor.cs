using ConCancha.Domain.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class PanelHeader
{
    [Inject] private CurrentSession Session { get; set; } = null!;

    [Parameter] public EventCallback OnSignOut { get; set; }

    private bool IsOwner => Session.Current?.Role == nameof(ClubRole.Owner);
}
