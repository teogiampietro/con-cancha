using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;

namespace ConCancha.Web.Extensions;

public static class ServiceCollectionExtensions
{
    private const string ApiBaseUrlKey = "Api:BaseUrl";
    private const string DefaultApiBaseUrl = "http://localhost:5089";
    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(15);

    public static IServiceCollection AddSessionServices(this IServiceCollection services)
    {
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
        services.AddScoped<CurrentSession>();
        services.AddScoped<AuthenticationStateProvider, SessionAuthenticationStateProvider>();
        services.AddScoped<ClientIpAccessor>();
        return services;
    }

    public static IServiceCollection AddApiClient(this IServiceCollection services)
    {
        services.AddHttpClient<ApiClient>((provider, client) =>
        {
            var baseUrl = provider.GetRequiredService<IConfiguration>()[ApiBaseUrlKey] ?? DefaultApiBaseUrl;
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = ApiTimeout;
        });
        return services;
    }

    /// <summary>Behind Railway's proxy the real IP arrives in X-Forwarded-For.</summary>
    public static IServiceCollection AddProxyForwardedHeaders(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
}
