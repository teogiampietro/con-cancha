using ConCancha.Api.Auth;
using ConCancha.Api.Subscriptions;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[Authorize(Roles = ClubRoles.Any)]
[AllowWithoutSubscription]
[Route("api/club/subscription")]
public class SubscriptionController(IClubSubscriptionService subscriptions) : ApiControllerBase
{
    /// <summary>The club's trial and payments, and whether the panel is open to it. Stays reachable when the trial is over.</summary>
    [HttpGet]
    [ProducesResponseType<ClubSubscriptionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken) =>
        ToActionResult(await subscriptions.GetCurrentAsync(cancellationToken), Ok);
}
