using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[Authorize(Roles = ClubRoles.Owner)]
[Route("api/club/setup")]
public class SetupController(ISetupService setup) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SetupProgressDto>> GetProgress(CancellationToken cancellationToken) =>
        await setup.GetProgressAsync(cancellationToken);

    /// <summary>Saves the wizard's draft so the owner can resume later.</summary>
    [HttpPut]
    public async Task<IActionResult> SaveProgress(SaveSetupProgressRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await setup.SaveProgressAsync(request, cancellationToken), NoContent);

    /// <summary>Creates the courts ("Pádel 1", "Pádel 2"…) with their prices and sets the club's hours.</summary>
    [HttpPost("complete")]
    public async Task<IActionResult> Complete(SetupAnswersDto answers, CancellationToken cancellationToken) =>
        ToActionResult(await setup.CompleteAsync(answers, cancellationToken), NoContent);
}
