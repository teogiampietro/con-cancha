using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Agenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[Authorize(Roles = ClubRoles.Any)]
public class AgendaController(IAgendaService agenda) : ApiControllerBase
{
    /// <summary>Every active court's slots for a day (club local date), free ones with their price.</summary>
    [HttpGet("api/agenda")]
    public async Task<ActionResult<AgendaDayDto>> GetDay([FromQuery] DateOnly date, CancellationToken cancellationToken) =>
        await agenda.GetDayAsync(date, cancellationToken);
}
