using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Platform;
using ConCancha.Domain.Platform;
using ConCancha.Infrastructure.Time;

namespace ConCancha.Infrastructure.Services;

public sealed class PlatformUsageService(
    ClubUsageReader usage, ClubBreakdownReader breakdown, ClubCustomerReader customers, TimeProvider clock) : IPlatformUsageService
{
    private const int FirstPage = 1;

    public async Task<Result<PlatformOverviewDto>> GetOverviewAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var calendar = CalendarAt(now);
        var period = ResolvePeriod(from, to, calendar);
        if (!period.IsSuccess) return Result<PlatformOverviewDto>.Failure(period.Error!);

        var clubs = await usage.ReadAsync(calendar, period.Value!, clubId: null, now, cancellationToken).ConfigureAwait(false);
        var ordered = clubs.OrderByDescending(club => club.BookingsInPeriod).ThenBy(club => club.Name).ToList();
        return Result<PlatformOverviewDto>.Success(new PlatformOverviewDto(
            ToDto(period.Value!), ToDto(period.Value!.Previous), Summarize(ordered), ordered));
    }

    public async Task<Result<ClubUsageDetailDto>> GetClubUsageAsync(
        Guid clubId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var calendar = CalendarAt(now);
        var period = ResolvePeriod(from, to, calendar);
        if (!period.IsSuccess) return Result<ClubUsageDetailDto>.Failure(period.Error!);

        var clubUsage = (await usage.ReadAsync(calendar, period.Value!, clubId, now, cancellationToken).ConfigureAwait(false)).SingleOrDefault();
        if (clubUsage is null)
            return Result<ClubUsageDetailDto>.Failure(ServiceError.NotFound(PlatformMessages.ClubNotFound));

        var courts = await breakdown.ReadCourtsAsync(clubId, period.Value!, cancellationToken).ConfigureAwait(false);
        var playedDays = await breakdown.ReadPlayedDaysAsync(clubId, period.Value!, cancellationToken).ConfigureAwait(false);
        return Result<ClubUsageDetailDto>.Success(new ClubUsageDetailDto(
            clubUsage, ToDto(period.Value!), ToDto(period.Value!.Previous), period.Value!.Granularity, courts, CountSeries(playedDays, period.Value!)));
    }

    public async Task<Result<PlatformCustomersPageDto>> GetClubCustomersAsync(
        Guid clubId, DateOnly? from, DateOnly? to, string? search, int page, CancellationToken cancellationToken)
    {
        var calendar = CalendarAt(clock.GetUtcNow());
        var period = ResolvePeriod(from, to, calendar);
        if (!period.IsSuccess) return Result<PlatformCustomersPageDto>.Failure(period.Error!);
        if (page < FirstPage) return Result<PlatformCustomersPageDto>.Failure(ServiceError.Validation("page", PlatformMessages.InvalidPage));

        var result = await customers.ReadPageAsync(clubId, period.Value!, search, page, cancellationToken).ConfigureAwait(false);
        return result is null
            ? Result<PlatformCustomersPageDto>.Failure(ServiceError.NotFound(PlatformMessages.ClubNotFound))
            : Result<PlatformCustomersPageDto>.Success(result);
    }

    private static UsageCalendar CalendarAt(DateTimeOffset now) => new(PlatformReporting.Zone.ToLocal(now).Date);

    private static Result<UsagePeriod> ResolvePeriod(DateOnly? from, DateOnly? to, UsageCalendar calendar)
    {
        if (from is null && to is null)
            return Result<UsagePeriod>.Success(UsagePeriod.Create(calendar.WeekStart, calendar.WeekEnd)!);
        if (from is null || to is null)
            return Result<UsagePeriod>.Failure(ServiceError.Validation("period", PlatformMessages.PeriodIncomplete));

        var period = UsagePeriod.Create(from.Value, to.Value);
        return period is null
            ? Result<UsagePeriod>.Failure(ServiceError.Validation("period", PlatformMessages.InvalidPeriod))
            : Result<UsagePeriod>.Success(period);
    }

    private static PeriodDto ToDto(UsagePeriod period) => new(period.From, period.To);

    private static PlatformTotalsDto Summarize(IReadOnlyList<ClubUsageDto> clubs) => new(
        clubs.Count,
        clubs.Count(club => club.Activity == ClubActivity.Active),
        clubs.Sum(club => club.ActiveCourts),
        clubs.Sum(club => club.Customers),
        clubs.Sum(club => club.BookingsInPeriod),
        clubs.Sum(club => club.BookingsInPreviousPeriod),
        clubs.Sum(club => club.OnlineBookingsInPeriod));

    private static IReadOnlyList<BookingCountDto> CountSeries(IReadOnlyList<DateOnly> playedDays, UsagePeriod period)
    {
        var perBucket = playedDays.CountBy(period.BucketOf).ToDictionary();
        return period.Buckets().Select(bucket => new BookingCountDto(bucket, perBucket.GetValueOrDefault(bucket))).ToList();
    }
}
