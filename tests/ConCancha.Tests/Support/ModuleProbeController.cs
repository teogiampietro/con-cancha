using ConCancha.Api.Modules;
using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Tests.Support;

/// <summary>Stands in for a future Buffet endpoint so the section gate can be tested before that module exists.</summary>
[ApiController]
[Authorize]
[RequiresModule(ClubModules.Buffet)]
public class ModuleProbeController : ControllerBase
{
    public const string Route = "/api/test/buffet";

    [HttpGet(Route)]
    public IActionResult Get() => Ok();
}
