using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Courts;
using ConCancha.Contracts.Pricing;
using ConCancha.Contracts.RecurringBookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[Authorize(Roles = ClubRoles.Any)]
public class CourtsController(ICourtService courts, IPriceRuleService priceRules, IRecurringBookingService recurringBookings)
    : ApiControllerBase
{
    [HttpGet("api/courts")]
    public async Task<ActionResult<IReadOnlyList<CourtDto>>> List(CancellationToken cancellationToken) =>
        Ok(await courts.ListAsync(cancellationToken));

    [HttpPost("api/courts")]
    [Authorize(Roles = ClubRoles.Owner)]
    [ProducesResponseType<CourtDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CourtRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await courts.CreateAsync(request, cancellationToken), court => StatusCode(StatusCodes.Status201Created, court));

    [HttpPut("api/courts/{courtId:guid}")]
    [Authorize(Roles = ClubRoles.Owner)]
    public async Task<IActionResult> Update(Guid courtId, CourtRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await courts.UpdateAsync(courtId, request, cancellationToken), Ok);

    [HttpGet("api/courts/{courtId:guid}/price-rules")]
    public async Task<IActionResult> ListPriceRules(Guid courtId, CancellationToken cancellationToken) =>
        ToActionResult(await priceRules.ListByCourtAsync(courtId, cancellationToken), Ok);

    [HttpPost("api/courts/{courtId:guid}/price-rules")]
    [Authorize(Roles = ClubRoles.Owner)]
    [ProducesResponseType<PriceRuleDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePriceRule(Guid courtId, PriceRuleRequest request, CancellationToken cancellationToken) =>
        ToActionResult(
            await priceRules.CreateAsync(courtId, request, cancellationToken),
            rule => StatusCode(StatusCodes.Status201Created, rule));

    [HttpPut("api/price-rules/{ruleId:guid}")]
    [Authorize(Roles = ClubRoles.Owner)]
    public async Task<IActionResult> UpdatePriceRule(Guid ruleId, PriceRuleRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await priceRules.UpdateAsync(ruleId, request, cancellationToken), Ok);

    [HttpDelete("api/price-rules/{ruleId:guid}")]
    [Authorize(Roles = ClubRoles.Owner)]
    public async Task<IActionResult> DeletePriceRule(Guid ruleId, CancellationToken cancellationToken) =>
        ToActionResult(await priceRules.DeleteAsync(ruleId, cancellationToken), NoContent);

    [HttpGet("api/courts/{courtId:guid}/recurring-bookings")]
    [Authorize(Roles = ClubRoles.Owner)]
    public async Task<IActionResult> ListRecurringBookings(Guid courtId, CancellationToken cancellationToken) =>
        ToActionResult(await recurringBookings.ListByCourtAsync(courtId, cancellationToken), Ok);

    [HttpPost("api/courts/{courtId:guid}/recurring-bookings")]
    [Authorize(Roles = ClubRoles.Owner)]
    [ProducesResponseType<RecurringBookingResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRecurringBooking(
        Guid courtId, RecurringBookingRequest request, CancellationToken cancellationToken) =>
        ToActionResult(
            await recurringBookings.CreateAsync(courtId, request, cancellationToken),
            result => StatusCode(StatusCodes.Status201Created, result));

    [HttpPut("api/courts/{courtId:guid}/recurring-bookings/{recurringId:guid}")]
    [Authorize(Roles = ClubRoles.Owner)]
    public async Task<IActionResult> UpdateRecurringBooking(
        Guid courtId, Guid recurringId, UpdateRecurringBookingRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await recurringBookings.UpdateAsync(courtId, recurringId, request, cancellationToken), Ok);

    [HttpDelete("api/courts/{courtId:guid}/recurring-bookings/{recurringId:guid}")]
    [Authorize(Roles = ClubRoles.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRecurringBooking(Guid courtId, Guid recurringId, CancellationToken cancellationToken) =>
        ToActionResult(await recurringBookings.DeleteAsync(courtId, recurringId, cancellationToken), NoContent);
}
