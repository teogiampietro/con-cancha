using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[Authorize(Roles = ClubRoles.Any)]
public class ReservationsController(IReservationService reservations) : ApiControllerBase
{
    /// <summary>Reservations (not blocks) by period, optionally only those still owing or booked online, with a name or phone search.</summary>
    [HttpGet("api/reservations")]
    public async Task<ActionResult<ReservationListDto>> List(
        [FromQuery] ReservationRange range = ReservationRange.Today,
        [FromQuery] ReservationFilter filter = ReservationFilter.All,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        await reservations.ListAsync(range, filter, search, cancellationToken);
}
