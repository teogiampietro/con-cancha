using ConCancha.Api.RateLimiting;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConCancha.Api.Controllers;

[Route("api/platform/auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class PlatformAuthController(IPlatformSignInService signIn) : ApiControllerBase
{
    private const string UnknownAddress = "unknown";

    /// <summary>
    /// Sign-in for ConCancha's own panel. The session carries no club. A wrong user or password gets 401; an address
    /// with too many failures gets 429 until its block ends.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(PlatformLoginRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await signIn.SignInAsync(request, ClientAddress, cancellationToken), Ok);

    private string ClientAddress => HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownAddress;
}
