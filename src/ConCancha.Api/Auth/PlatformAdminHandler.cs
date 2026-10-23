using ConCancha.Contracts;
using ConCancha.Contracts.Platform;
using ConCancha.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;

namespace ConCancha.Api.Auth;

public sealed class PlatformAdminHandler(PlatformCredentials credentials) : AuthorizationHandler<PlatformAdminRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PlatformAdminRequirement requirement)
    {
        var fingerprint = context.User.FindFirst(SessionClaims.PlatformCredential)?.Value;
        if (context.User.IsInRole(PlatformRoles.Admin) && fingerprint is not null && fingerprint == credentials.CurrentFingerprint)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
