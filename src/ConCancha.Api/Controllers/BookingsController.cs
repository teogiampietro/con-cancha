using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[Authorize(Roles = ClubRoles.Any)]
[Route("api/bookings")]
public class BookingsController(IBookingService bookings) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<BookingDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReservation(CreateBookingRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await bookings.CreateReservationAsync(request, cancellationToken), Created);

    [HttpPost("blocks")]
    [ProducesResponseType<BookingDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBlock(CreateBlockRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await bookings.CreateBlockAsync(request, cancellationToken), Created);

    [HttpGet("{bookingId:guid}")]
    public async Task<IActionResult> Get(Guid bookingId, CancellationToken cancellationToken) =>
        ToActionResult(await bookings.GetAsync(bookingId, cancellationToken), Ok);

    [HttpPost("{bookingId:guid}/pay")]
    public async Task<IActionResult> MarkPaid(Guid bookingId, MarkPaidRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await bookings.MarkPaidAsync(bookingId, request, cancellationToken), Ok);

    [HttpPost("{bookingId:guid}/played")]
    public async Task<IActionResult> MarkPlayed(Guid bookingId, CancellationToken cancellationToken) =>
        ToActionResult(await bookings.MarkPlayedAsync(bookingId, cancellationToken), Ok);

    [HttpPost("{bookingId:guid}/no-show")]
    public async Task<IActionResult> MarkNoShow(Guid bookingId, CancellationToken cancellationToken) =>
        ToActionResult(await bookings.MarkNoShowAsync(bookingId, cancellationToken), Ok);

    [HttpPut("{bookingId:guid}/price")]
    public async Task<IActionResult> ChangePrice(Guid bookingId, ChangePriceRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await bookings.ChangePriceAsync(bookingId, request, cancellationToken), Ok);

    [HttpPost("{bookingId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid bookingId, CancellationToken cancellationToken) =>
        ToActionResult(await bookings.CancelAsync(bookingId, cancellationToken), Ok);

    private IActionResult Created(BookingDto booking) => StatusCode(StatusCodes.Status201Created, booking);
}
