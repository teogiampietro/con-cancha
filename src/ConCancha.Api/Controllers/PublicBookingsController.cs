using ConCancha.Api.RateLimiting;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConCancha.Api.Controllers;

[AllowAnonymous]
[Route("api/public/clubs/{slug}")]
public class PublicBookingsController(IPublicBookingService publicBookings) : ApiControllerBase
{
    /// <summary>Slots of every active court for a day (today and the next six). Shows no customer data.</summary>
    [HttpGet("availability")]
    [EnableRateLimiting(RateLimitPolicies.Query)]
    public async Task<IActionResult> GetDay(string slug, [FromQuery] DateOnly date, CancellationToken cancellationToken) =>
        ToActionResult(await publicBookings.GetDayAsync(slug, date, cancellationToken), Ok);

    /// <summary>Books a slot. The response carries the secret of the player's management link, shown only now.</summary>
    [HttpPost("bookings")]
    [EnableRateLimiting(RateLimitPolicies.PublicBooking)]
    [ProducesResponseType<PublicBookingCreatedDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(string slug, PublicBookingRequest request, CancellationToken cancellationToken) =>
        ToActionResult(
            await publicBookings.BookAsync(slug, request, cancellationToken),
            created => StatusCode(StatusCodes.Status201Created, created));

    [HttpGet("bookings/{token}")]
    [EnableRateLimiting(RateLimitPolicies.Query)]
    public async Task<IActionResult> GetBooking(string slug, string token, CancellationToken cancellationToken) =>
        ToActionResult(await publicBookings.GetBookingAsync(slug, token, cancellationToken), Ok);

    /// <summary>Cancels with the management link, only until the club's cancellation window closes.</summary>
    [HttpPost("bookings/{token}/cancel")]
    [EnableRateLimiting(RateLimitPolicies.PublicBooking)]
    public async Task<IActionResult> Cancel(string slug, string token, CancellationToken cancellationToken) =>
        ToActionResult(await publicBookings.CancelAsync(slug, token, cancellationToken), Ok);
}
