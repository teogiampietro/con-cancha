using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Reminders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[Authorize(Roles = ClubRoles.Any)]
[Route("api/reminders")]
public class RemindersController(IReminderService reminders) : ApiControllerBase
{
    /// <summary>Turns of tomorrow or today that can be reminded by WhatsApp (they have a valid phone and were not played or missed).</summary>
    [HttpGet]
    public async Task<ActionResult<ReminderListDto>> List([FromQuery] ReminderDay day = ReminderDay.Tomorrow, CancellationToken cancellationToken = default) =>
        await reminders.ListAsync(day, cancellationToken);

    [HttpPost("{bookingId:guid}/sent")]
    public async Task<IActionResult> MarkSent(Guid bookingId, CancellationToken cancellationToken) =>
        ToActionResult(await reminders.MarkSentAsync(bookingId, cancellationToken), NoContent);
}
