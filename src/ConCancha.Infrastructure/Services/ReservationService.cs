using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Reservations;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Customers;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Services.Mapping;
using ConCancha.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

public sealed class ReservationService(ConCanchaDbContext db, CurrentClubProvider currentClub, TimeProvider clock) : IReservationService
{
    private const int MaxItems = 200;
    private const int WeekDays = 7;
    private const int MonthDays = 30;

    public async Task<ReservationListDto> ListAsync(
        ReservationRange range, ReservationFilter filter, string? search, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        if (club is null) return new ReservationListDto([], false);

        var zone = ClubTimeZone.For(club);
        var query = db.Bookings.AsNoTracking().Include(booking => booking.Court)
            .Where(booking => booking.Kind == BookingKind.Reservation);
        query = await ApplySearchAsync(ApplyFilter(ApplyRange(query, range, zone), filter), search, cancellationToken).ConfigureAwait(false);
        query = range == ReservationRange.History
            ? query.OrderByDescending(booking => booking.StartsAt)
            : query.OrderBy(booking => booking.StartsAt);

        var found = await query.Take(MaxItems + 1).ToListAsync(cancellationToken).ConfigureAwait(false);
        var items = found.Take(MaxItems).Select(booking => new ReservationItemDto(BookingMapper.ToDto(booking, zone), booking.Court!.Name)).ToList();
        return new ReservationListDto(items, found.Count > MaxItems);
    }

    private IQueryable<Booking> ApplyRange(IQueryable<Booking> query, ReservationRange range, ClubTimeZone zone)
    {
        var today = zone.ToLocal(clock.GetUtcNow()).Date;
        var startOfToday = zone.ToUtc(today, 0);
        return range switch
        {
            ReservationRange.Today => query.Where(b => b.StartsAt >= startOfToday && b.StartsAt < zone.ToUtc(today.AddDays(1), 0)),
            ReservationRange.Next7Days => query.Where(b => b.StartsAt >= startOfToday && b.StartsAt < zone.ToUtc(today.AddDays(WeekDays + 1), 0)),
            ReservationRange.Next30Days => query.Where(b => b.StartsAt >= startOfToday && b.StartsAt < zone.ToUtc(today.AddDays(MonthDays + 1), 0)),
            _ => query.Where(b => b.StartsAt < startOfToday),
        };
    }

    private static IQueryable<Booking> ApplyFilter(IQueryable<Booking> query, ReservationFilter filter) => filter switch
    {
        ReservationFilter.ToCollect => query.WhereToCollect(),
        ReservationFilter.Online => query.Where(booking => booking.Origin == BookingOrigin.Online),
        _ => query,
    };

    private async Task<IQueryable<Booking>> ApplySearchAsync(IQueryable<Booking> query, string? search, CancellationToken cancellationToken)
    {
        var text = search?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(text)) return query;

        var digits = new string(text.Where(char.IsDigit).ToArray());
        var exactPhone = PhoneNormalizer.Normalize(text) ?? digits;
        var customerIds = digits.Length == 0
            ? []
            : await db.Customers.Where(c => c.NormalizedPhone == exactPhone || c.NormalizedPhone.Contains(digits))
                .Select(c => c.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

        return query.Where(booking => booking.CustomerName!.ToLower().Contains(text)
            || booking.CustomerPhone!.Contains(text)
            || (booking.CustomerId != null && customerIds.Contains(booking.CustomerId.Value)));
    }
}
