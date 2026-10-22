using ConCancha.Application.Results;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;

namespace ConCancha.Application.Abstractions;

/// <summary>How a club looks and what it shows: logo, colors and optional menu sections.</summary>
public interface IClubAppearanceService
{
    Task<Result<ClubDto>> UpdateColorsAsync(UpdateClubColorsRequest request, CancellationToken cancellationToken);

    Task<Result<ClubDto>> UpdateLogoAsync(Stream content, CancellationToken cancellationToken);

    Task<Result<ClubDto>> RemoveLogoAsync(CancellationToken cancellationToken);

    Task<Result<ClubDto>> UpdateModulesAsync(UpdateClubModulesRequest request, CancellationToken cancellationToken);

    Task<Result<LogoFile>> OpenPublicLogoAsync(string slug, CancellationToken cancellationToken);

    Task<bool> IsModuleEnabledAsync(ClubModules module, CancellationToken cancellationToken);
}
