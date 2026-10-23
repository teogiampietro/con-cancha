using ConCancha.Api.RateLimiting;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConCancha.Api.Controllers;

public class ClubsController(IClubService clubs) : ApiControllerBase
{
    /// <summary>Steps 2 and 3 of sign-up: creates the club and makes its creator the owner. Returns a new session that already carries the club.</summary>
    [HttpPost("api/clubs")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateClub(CreateClubRequest request, CancellationToken cancellationToken) =>
        ToActionResult(
            await clubs.CreateClubForUserAsync(CurrentUserId, request, cancellationToken),
            session => StatusCode(StatusCodes.Status201Created, session));

    /// <summary>Live slug availability, as the mock-up does while typing.</summary>
    [HttpGet("api/clubs/slug-availability")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Query)]
    public async Task<ActionResult<SlugAvailabilityResponse>> GetSlugAvailability(
        [FromQuery] string slug, CancellationToken cancellationToken) =>
        await clubs.GetSlugAvailabilityAsync(slug, cancellationToken);

    /// <summary>The session's club (the token's).</summary>
    [HttpGet("api/club")]
    [Authorize]
    public async Task<IActionResult> GetCurrentClub(CancellationToken cancellationToken) =>
        ToActionResult(await clubs.GetCurrentClubAsync(cancellationToken), Ok);

    /// <summary>Changes the club-wide opening and closing time. Owner only.</summary>
    [HttpPut("api/club/hours")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    public async Task<IActionResult> UpdateHours(UpdateClubHoursRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await clubs.UpdateHoursAsync(request, cancellationToken), Ok);

    /// <summary>Changes the club's name and phone. The slug never changes, so shared links keep working. Owner only.</summary>
    [HttpPut("api/club/profile")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    public async Task<IActionResult> UpdateProfile(UpdateClubProfileRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await clubs.UpdateProfileAsync(request, cancellationToken), Ok);

    /// <summary>Changes how many hours before the turn a player can still cancel online. Owner only.</summary>
    [HttpPut("api/club/cancellation")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    public async Task<IActionResult> UpdateCancellation(UpdateCancellationRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await clubs.UpdateCancellationAsync(request, cancellationToken), Ok);

    /// <summary>Public data of a club by its link: what a player without an account sees.</summary>
    [HttpGet("api/public/clubs/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicClub(string slug, CancellationToken cancellationToken) =>
        ToActionResult(await clubs.ResolvePublicClubBySlugAsync(slug, cancellationToken), Ok);
}
