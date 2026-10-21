using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Cash;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[Authorize(Roles = ClubRoles.Any)]
[Route("api/cash/{date}")]
public class CashController(ICashService cash) : ApiControllerBase
{
    /// <summary>The day's takings by payment method and what is still owed. <c>date</c> is yyyy-MM-dd, in the club's time zone.</summary>
    [HttpGet]
    public async Task<ActionResult<CashDayDto>> GetDay(DateOnly date, CancellationToken cancellationToken) =>
        await cash.GetDayAsync(date, cancellationToken);

    /// <summary>Marks the day as closed. It only reports: the day's turns can still change.</summary>
    [HttpPost("close")]
    public async Task<ActionResult<CashDayDto>> Close(DateOnly date, CancellationToken cancellationToken) =>
        await cash.CloseDayAsync(date, cancellationToken);

    [HttpPost("reopen")]
    public async Task<ActionResult<CashDayDto>> Reopen(DateOnly date, CancellationToken cancellationToken) =>
        await cash.ReopenDayAsync(date, cancellationToken);
}
