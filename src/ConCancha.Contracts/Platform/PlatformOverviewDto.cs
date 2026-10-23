namespace ConCancha.Contracts.Platform;

/// <param name="Period">The period the figures cover.</param>
/// <param name="PreviousPeriod">The period right before it, of the same length.</param>
public record PlatformOverviewDto(
    PeriodDto Period,
    PeriodDto PreviousPeriod,
    PlatformTotalsDto Totals,
    IReadOnlyList<ClubUsageDto> Clubs);
