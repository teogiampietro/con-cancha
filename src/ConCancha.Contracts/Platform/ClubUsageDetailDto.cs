using ConCancha.Domain.Platform;

namespace ConCancha.Contracts.Platform;

/// <param name="Series">One entry per day, or per week (Monday) when the period is longer than a month.</param>
public record ClubUsageDetailDto(
    ClubUsageDto Usage,
    PeriodDto Period,
    PeriodDto PreviousPeriod,
    UsageGranularity Granularity,
    IReadOnlyList<CourtUsageDto> Courts,
    IReadOnlyList<BookingCountDto> Series);
