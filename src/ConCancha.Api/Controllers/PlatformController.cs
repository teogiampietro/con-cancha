using ConCancha.Api.Auth;
using ConCancha.Api.RateLimiting;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConCancha.Api.Controllers;

[Route("api/platform")]
[Authorize(Policy = PlatformPolicies.Admin)]
[EnableRateLimiting(RateLimitPolicies.Query)]
public class PlatformController(IPlatformUsageService usage) : ApiControllerBase
{
    /// <summary>Every club with its courts, customers and turns in the period, against the period right before it.</summary>
    [HttpGet("overview")]
    [ProducesResponseType<PlatformOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverview([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        ToActionResult(await usage.GetOverviewAsync(from, to, cancellationToken), Ok);

    /// <summary>One club: its courts, and its turns per day or per week across the period.</summary>
    [HttpGet("clubs/{clubId:guid}")]
    [ProducesResponseType<ClubUsageDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClub(
        Guid clubId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        ToActionResult(await usage.GetClubUsageAsync(clubId, from, to, cancellationToken), Ok);

    /// <summary>A club's customers with their turns in the period, latest turn first. Searches by name or phone.</summary>
    [HttpGet("clubs/{clubId:guid}/customers")]
    [ProducesResponseType<PlatformCustomersPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomers(
        Guid clubId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? search,
        [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        ToActionResult(await usage.GetClubCustomersAsync(clubId, from, to, search, page, cancellationToken), Ok);
}
