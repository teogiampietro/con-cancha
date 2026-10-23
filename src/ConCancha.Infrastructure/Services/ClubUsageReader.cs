using ConCancha.Contracts.Platform;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Platform;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

/// <summary>
/// Usage figures per club, counted in the database with one grouped query per figure, so the cost follows the number
/// of clubs and not the number of turns. Reads across clubs, hence <c>IgnoreQueryFilters</c>.
/// </summary>
public sealed class ClubUsageReader(ConCanchaDbContext db)
{
    /// <param name="clubId">One club, or every club when <c>null</c>.</param>
    public async Task<IReadOnlyList<ClubUsageDto>> ReadAsync(
        UsageCalendar calendar, UsagePeriod period, Guid? clubId, CancellationToken cancellationToken)
    {
        var clubs = await LoadClubsAsync(clubId, cancellationToken).ConfigureAwait(false);
        if (clubs.Count == 0) return [];

        var clubIds = clubs.Select(club => club.Id).ToList();
        var bookings = await CountBookingsAsync(clubIds, calendar, period, cancellationToken).ConfigureAwait(false);
        var courts = await CountActiveCourtsAsync(clubIds, cancellationToken).ConfigureAwait(false);
        var customers = await CountCustomersAsync(clubIds, cancellationToken).ConfigureAwait(false);
        var owners = await FindOwnerEmailsAsync(clubIds, cancellationToken).ConfigureAwait(false);
        var lastBookings = await FindLastBookingsAsync(clubIds, cancellationToken).ConfigureAwait(false);

        return clubs
            .Select(club => ToUsage(
                club, calendar, period,
                bookings.GetValueOrDefault(club.Id) ?? BookingCounts.None(club.Id),
                new ClubFacts(
                    courts.GetValueOrDefault(club.Id),
                    customers.GetValueOrDefault(club.Id),
                    owners.GetValueOrDefault(club.Id),
                    lastBookings.GetValueOrDefault(club.Id))))
            .ToList();
    }

    private Task<List<ClubRow>> LoadClubsAsync(Guid? clubId, CancellationToken cancellationToken) =>
        db.Clubs.AsNoTracking()
            .Where(club => clubId == null || club.Id == clubId)
            .Select(club => new ClubRow(club.Id, club.Name, club.Slug, club.Phone, club.CreatedAt))
            .ToListAsync(cancellationToken);

    private Task<Dictionary<Guid, BookingCounts>> CountBookingsAsync(
        IReadOnlyList<Guid> clubIds, UsageCalendar calendar, UsagePeriod period, CancellationToken cancellationToken)
    {
        var at = BookingWindow.Of(calendar, period);
        return db.Bookings.IgnoreQueryFilters().AsNoTracking().WhereCountedTurn()
            .Where(booking => clubIds.Contains(booking.ClubId) && booking.StartsAt >= at.Earliest && booking.StartsAt < at.Latest)
            .GroupBy(booking => booking.ClubId)
            .Select(group => new BookingCounts(
                group.Key,
                group.Count(booking => booking.StartsAt >= at.PeriodFrom && booking.StartsAt < at.PeriodTo),
                group.Count(booking => booking.StartsAt >= at.PreviousFrom && booking.StartsAt < at.PeriodFrom),
                group.Count(booking => booking.StartsAt >= at.PeriodFrom && booking.StartsAt < at.PeriodTo && booking.Origin == BookingOrigin.Online),
                group.Count(booking => booking.StartsAt >= at.ActivityFrom && booking.StartsAt < at.ActivityTo)))
            .ToDictionaryAsync(counts => counts.ClubId, cancellationToken);
    }

    private Task<Dictionary<Guid, int>> CountActiveCourtsAsync(IReadOnlyList<Guid> clubIds, CancellationToken cancellationToken) =>
        db.Courts.IgnoreQueryFilters().AsNoTracking()
            .Where(court => clubIds.Contains(court.ClubId) && court.IsActive)
            .GroupBy(court => court.ClubId)
            .Select(group => new { ClubId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(entry => entry.ClubId, entry => entry.Count, cancellationToken);

    private Task<Dictionary<Guid, int>> CountCustomersAsync(IReadOnlyList<Guid> clubIds, CancellationToken cancellationToken) =>
        db.Customers.IgnoreQueryFilters().AsNoTracking()
            .Where(customer => clubIds.Contains(customer.ClubId))
            .GroupBy(customer => customer.ClubId)
            .Select(group => new { ClubId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(entry => entry.ClubId, entry => entry.Count, cancellationToken);

    /// <remarks>A club has one owner today; if it ever has more, the first one to join is shown.</remarks>
    private async Task<Dictionary<Guid, string?>> FindOwnerEmailsAsync(IReadOnlyList<Guid> clubIds, CancellationToken cancellationToken)
    {
        var owners = await db.ClubMembers.IgnoreQueryFilters().AsNoTracking()
            .Where(member => clubIds.Contains(member.ClubId) && member.Role == ClubRole.Owner)
            .Join(db.Users, member => member.UserId, user => user.Id, (member, user) => new { member.ClubId, member.CreatedAt, user.Email })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return owners
            .GroupBy(owner => owner.ClubId)
            .ToDictionary(group => group.Key, group => group.OrderBy(owner => owner.CreatedAt).First().Email);
    }

    private async Task<Dictionary<Guid, DateOnly?>> FindLastBookingsAsync(IReadOnlyList<Guid> clubIds, CancellationToken cancellationToken)
    {
        var lastCreated = await db.Bookings.IgnoreQueryFilters().AsNoTracking()
            .Where(booking => clubIds.Contains(booking.ClubId) && booking.Kind == BookingKind.Reservation)
            .GroupBy(booking => booking.ClubId)
            .Select(group => new { ClubId = group.Key, CreatedAt = group.Max(booking => booking.CreatedAt) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return lastCreated.ToDictionary(entry => entry.ClubId, entry => (DateOnly?)PlatformReporting.Zone.ToLocal(entry.CreatedAt).Date);
    }

    private static ClubUsageDto ToUsage(ClubRow club, UsageCalendar calendar, UsagePeriod period, BookingCounts bookings, ClubFacts facts)
    {
        var createdOn = PlatformReporting.Zone.ToLocal(club.CreatedAt).Date;
        var activity = ClubActivityRules.Classify(createdOn, calendar.Today, bookings.RecentWeeks);

        return new ClubUsageDto(
            club.Id, club.Name, club.Slug, club.Phone, facts.OwnerEmail, createdOn, activity, facts.ActiveCourts, facts.Customers,
            bookings.InPeriod, bookings.InPreviousPeriod, bookings.Online, period.AveragePerDay(bookings.InPeriod), facts.LastBookingOn);
    }

    /// <summary>Every UTC instant the usage figures compare against, so one query covers all of them.</summary>
    private sealed record BookingWindow(
        DateTimeOffset PeriodFrom, DateTimeOffset PeriodTo, DateTimeOffset PreviousFrom, DateTimeOffset ActivityFrom, DateTimeOffset ActivityTo)
    {
        public DateTimeOffset Earliest => PreviousFrom < ActivityFrom ? PreviousFrom : ActivityFrom;

        public DateTimeOffset Latest => PeriodTo > ActivityTo ? PeriodTo : ActivityTo;

        public static BookingWindow Of(UsageCalendar calendar, UsagePeriod period) => new(
            PlatformReporting.StartOf(period.From),
            PlatformReporting.StartOf(period.ToExclusive),
            PlatformReporting.StartOf(period.Previous.From),
            PlatformReporting.StartOf(calendar.LastWeekStart),
            PlatformReporting.StartOf(calendar.NextWeekStart));
    }

    private sealed record ClubRow(Guid Id, string Name, string Slug, string? Phone, DateTimeOffset CreatedAt);

    private sealed record ClubFacts(int ActiveCourts, int Customers, string? OwnerEmail, DateOnly? LastBookingOn);

    /// <param name="RecentWeeks">Turns of the current and the previous week, which decide whether the club is active.</param>
    private sealed record BookingCounts(Guid ClubId, int InPeriod, int InPreviousPeriod, int Online, int RecentWeeks)
    {
        public static BookingCounts None(Guid clubId) => new(clubId, 0, 0, 0, 0);
    }
}
