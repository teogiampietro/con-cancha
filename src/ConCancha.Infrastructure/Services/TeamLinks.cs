using Microsoft.Extensions.Options;

namespace ConCancha.Infrastructure.Services;

public sealed class TeamLinks(IOptions<WebAppOptions> webOptions)
{
    private const string AcceptInvitationPath = "/panel/invitacion";
    private const string PanelPath = "/panel";

    public string PanelLink() => BaseUrl() + PanelPath;

    public string AcceptInvitationLink(string email, string token) =>
        $"{BaseUrl()}{AcceptInvitationPath}?mail={Uri.EscapeDataString(email)}&token={token}";

    private string BaseUrl() => webOptions.Value.BaseUrl.TrimEnd('/');
}
