using ConCancha.Api.Auth;
using ConCancha.Api.RateLimiting;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConCancha.Api.Controllers;

[Route("api/platform/clubs/{clubId:guid}")]
[Authorize(Policy = PlatformPolicies.Admin)]
[EnableRateLimiting(RateLimitPolicies.Query)]
public class PlatformSubscriptionsController(IPlatformSubscriptionService subscriptions) : ApiControllerBase
{
    /// <summary>Adds days to the club's trial. Counts from today when the trial is already over.</summary>
    [HttpPost("trial/extension")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExtendTrial(Guid clubId, ExtendTrialRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await subscriptions.ExtendTrialAsync(clubId, request.Days, cancellationToken), NoContent);

    /// <summary>Records a monthly payment received outside the app. It covers 30 days more.</summary>
    [HttpPost("subscription/payment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkPaid(Guid clubId, CancellationToken cancellationToken) =>
        ToActionResult(await subscriptions.MarkPaidAsync(clubId, cancellationToken), NoContent);

    /// <summary>Emails the club's owner about an overdue trial or payment.</summary>
    [HttpPost("subscription/regularization-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendRegularizationEmail(Guid clubId, CancellationToken cancellationToken) =>
        ToActionResult(await subscriptions.SendRegularizationEmailAsync(clubId, cancellationToken), NoContent);
}
