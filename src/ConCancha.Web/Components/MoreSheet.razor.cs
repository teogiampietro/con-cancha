using ConCancha.Contracts.Clubs;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class MoreSheet
{
    [Parameter, EditorRequired] public ClubDto Club { get; set; } = null!;
    [Parameter] public string? Role { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnSignOut { get; set; }
}
