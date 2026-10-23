using ConCancha.Contracts.Platform;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Platform;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

/// <summary>What the club detail shows beyond the totals: turns per court and the turn starts behind the chart.</summary>
public sealed class ClubBreakdownReader(ConCanchaDbContext db)
{
    public async Task<IReadOnlyList<CourtUsageDto>> ReadCourtsAsync(Guid clubId, UsagePeriod period, CancellationToken cancellationToken)
    {
        var (from, to) = PlatformReporting.UtcRange(period.From, period.ToExclusive);
        var bookingsByCourt = await db.Bookings.IgnoreQueryFilters().AsNoTracking().WhereCountedTurn()
            .Where(booking => booking.ClubId == clubId && booking.StartsAt >= from && booking.StartsAt < to)
            .GroupBy(booking => booking.CourtId)
            .Select(group => new { CourtId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(entry => entry.CourtId, entry => entry.Count, cancellationToken).ConfigureAwait(false);

        var courts = await db.Courts.IgnoreQueryFilters().AsNoTracking()
            .Where(court => court.ClubId == clubId)
            .OrderByDescending(court => court.IsActive).ThenBy(court => court.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return courts
            .Select(court => new CourtUsageDto(court.Id, court.Name, court.Sport, court.IsActive, bookingsByCourt.GetValueOrDefault(court.Id)))
            .ToList();
    }

    /// <returns>The local day each counted turn of the period is played on.</returns>
    public async Task<IReadOnlyList<DateOnly>> ReadPlayedDaysAsync(Guid clubId, UsagePeriod period, CancellationToken cancellationToken)
    {
        var (from, to) = PlatformReporting.UtcRange(period.From, period.ToExclusive);
        var starts = await db.Bookings.IgnoreQueryFilters().AsNoTracking().WhereCountedTurn()
            .Where(booking => booking.ClubId == clubId && booking.StartsAt >= from && booking.StartsAt < to)
            .Select(booking => booking.StartsAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return starts.Select(start => PlatformReporting.Zone.ToLocal(start).Date).ToList();
    }
}
