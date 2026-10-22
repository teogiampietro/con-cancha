using ConCancha.Api.RateLimiting;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Accounts;
using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConCancha.Api.Controllers;

[Route("api/auth")]
[Authorize(Roles = nameof(ClubRole.Owner))]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class CurrentAccountController(IAccountService accounts) : ApiControllerBase
{
    /// <summary>Changes the password of the signed-in owner. Rate limited like login, since it checks the current password.</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await accounts.ChangePasswordAsync(CurrentUserId, request, cancellationToken), NoContent);
}
