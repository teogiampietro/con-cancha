using ConCancha.Application.Abstractions;
using ConCancha.Contracts;

namespace ConCancha.Api.Tenancy;

/// <summary>If the token carries a club, makes it the current club: from there global filters apply to that club.</summary>
public sealed class ClubTenancyMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, ITenantContext tenant)
    {
        if (Guid.TryParse(context.User.FindFirst(SessionClaims.ClubId)?.Value, out var clubId))
            tenant.SetClub(clubId);

        return next(context);
    }
}
