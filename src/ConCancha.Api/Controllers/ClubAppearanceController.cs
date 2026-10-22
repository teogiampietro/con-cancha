using ConCancha.Api.RateLimiting;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConCancha.Api.Controllers;

public class ClubAppearanceController(IClubAppearanceService appearance) : ApiControllerBase
{
    private const long LogoRequestLimitBytes = LogoRules.MaxBytes + 64 * 1024;
    private const string CacheControlValue = "public, max-age=86400";

    /// <summary>Changes the colors the public site uses. Colors too dark to read are rejected. Owner only.</summary>
    [HttpPut("api/club/colors")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    public async Task<IActionResult> UpdateColors(UpdateClubColorsRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await appearance.UpdateColorsAsync(request, cancellationToken), Ok);

    /// <summary>Uploads the club logo (PNG or JPG, up to 2 MB); the content decides the type. Owner only.</summary>
    [HttpPut("api/club/logo")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    [RequestSizeLimit(LogoRequestLimitBytes)]
    public async Task<IActionResult> UpdateLogo(IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        return ToActionResult(await appearance.UpdateLogoAsync(content, cancellationToken), Ok);
    }

    /// <summary>Removes the club logo. Owner only.</summary>
    [HttpDelete("api/club/logo")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    public async Task<IActionResult> RemoveLogo(CancellationToken cancellationToken) =>
        ToActionResult(await appearance.RemoveLogoAsync(cancellationToken), Ok);

    /// <summary>Shows or hides the optional menu sections. Owner only.</summary>
    [HttpPut("api/club/modules")]
    [Authorize(Roles = nameof(ClubRole.Owner))]
    public async Task<IActionResult> UpdateModules(UpdateClubModulesRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await appearance.UpdateModulesAsync(request, cancellationToken), Ok);

    /// <summary>The logo shown on the public site of the club.</summary>
    [HttpGet("api/public/clubs/{slug}/logo")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Query)]
    public async Task<IActionResult> GetPublicLogo(string slug, CancellationToken cancellationToken)
    {
        var result = await appearance.OpenPublicLogoAsync(slug, cancellationToken);
        if (!result.IsSuccess) return ToProblem(result.Error!);

        Response.Headers.CacheControl = CacheControlValue;
        return File(result.Value!.Content, result.Value.ContentType);
    }
}
