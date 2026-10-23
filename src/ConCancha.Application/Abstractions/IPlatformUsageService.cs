using ConCancha.Application.Results;
using ConCancha.Contracts.Platform;

namespace ConCancha.Application.Abstractions;

/// <summary>Usage of every club, for ConCancha's own team. Reads across clubs, so it never runs under a club's filter.</summary>
public interface IPlatformUsageService
{
    /// <param name="from">First day of the period; both dates missing means the current week.</param>
    /// <param name="to">Last day of the period, included.</param>
    Task<Result<PlatformOverviewDto>> GetOverviewAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<Result<ClubUsageDetailDto>> GetClubUsageAsync(Guid clubId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    /// <param name="page">1-based; each page holds 50 customers.</param>
    Task<Result<PlatformCustomersPageDto>> GetClubCustomersAsync(
        Guid clubId, DateOnly? from, DateOnly? to, string? search, int page, CancellationToken cancellationToken);
}
