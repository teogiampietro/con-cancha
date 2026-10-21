using Microsoft.AspNetCore.HttpOverrides;

namespace ConCancha.Api.Extensions;

public static class ForwardedHeadersExtensions
{
    /// <summary>Behind Railway's proxy the real IP arrives in X-Forwarded-For.</summary>
    public static IServiceCollection AddProxyForwardedHeaders(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
}
