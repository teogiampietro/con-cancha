using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class Routes
{
    [Inject] private ClientIpAccessor ClientIpAccessor { get; set; } = null!;

    [Parameter] public string? ClientIp { get; set; }

    protected override void OnInitialized() => ClientIpAccessor.Address = ClientIp;
}
