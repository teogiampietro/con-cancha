using ConCancha.Infrastructure.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConCancha.Api.Auth;

/// <summary>Warns once at startup when the platform admin is not configured; the panel then stays closed.</summary>
public sealed class PlatformCredentialsWarning(PlatformCredentials credentials, ILogger<PlatformCredentialsWarning> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!credentials.IsConfigured)
            logger.LogWarning("Platform admin is not configured: set Platform__AdminUser and Platform__AdminPasswordHash. The platform panel stays closed.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
