using ConCancha.Application.Abstractions;
using ConCancha.Contracts;

namespace ConCancha.Api.Tenancy;

/// <summary>
/// If the token carries a club, makes it the current club: from there global filters apply to that club.
/// A token outlives a removed member, so the membership is checked on every request.
/// </summary>
public sealed class ClubTenancyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenant, IMembershipVerifier memberships)
    {
        if (Guid.TryParse(context.User.FindFirst(SessionClaims.ClubId)?.Value, out var clubId))
        {
            if (!await HasMembershipAsync(context, memberships, clubId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            tenant.SetClub(clubId);
        }

        await next(context);
    }

    private static Task<bool> HasMembershipAsync(HttpContext context, IMembershipVerifier memberships, Guid clubId)
    {
        var userId = context.User.FindFirst(SessionClaims.UserId)?.Value ?? string.Empty;
        var role = context.User.FindFirst(SessionClaims.Role)?.Value ?? string.Empty;
        return memberships.HasMembershipAsync(userId, clubId, role, context.RequestAborted);
    }
}
