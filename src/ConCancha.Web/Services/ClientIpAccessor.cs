namespace ConCancha.Web.Services;

/// <summary>
/// Real IP of the site visitor. Blazor Server calls the API from the server, so the IP is forwarded in
/// X-Forwarded-For so the API's rate limit counts per person and not per server.
/// </summary>
public sealed class ClientIpAccessor
{
    public string? Address { get; set; }
}
