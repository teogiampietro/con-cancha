using ConCancha.Contracts.Clubs;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class PanelNav
{
    [Parameter, EditorRequired] public ClubDto Club { get; set; } = null!;
    [Parameter] public string? Role { get; set; }
    [Parameter] public EventCallback OnSignOut { get; set; }

    private string Initial => Club.Name.Length > 0 ? Club.Name[..1].ToUpperInvariant() : "?";
}
