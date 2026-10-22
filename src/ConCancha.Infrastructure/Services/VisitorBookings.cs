using ConCancha.Contracts.Public;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

/// <summary>The bookings a browser made, found by the hash of its visitor token. Callers must have entered a club first.</summary>
public sealed class VisitorBookings(ConCanchaDbContext db, TimeProvider clock)
{
    public static string? HashOrNull(string? visitorToken) =>
        VisitorToken.IsValid(visitorToken) ? ManagementToken.Hash(visitorToken!) : null;

    public async Task<IReadOnlySet<Guid>> GetIdsOnDayAsync(
        Club club, DateOnly date, string? visitorToken, CancellationToken cancellationToken)
    {
        if (HashOrNull(visitorToken) is not { } hash) return new HashSet<Guid>();

        var (start, end) = ClubTimeZone.For(club).GetDayRange(date);
        var ids = await db.Bookings.AsNoTracking()
            .Where(booking => booking.VisitorTokenHash == hash && booking.Status == BookingStatus.Confirmed
                && booking.StartsAt >= start && booking.StartsAt < end)
            .Select(booking => booking.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return ids.ToHashSet();
    }

    public async Task<IReadOnlyList<Booking>> ListUpcomingAsync(string? visitorToken, CancellationToken cancellationToken)
    {
        if (HashOrNull(visitorToken) is not { } hash) return [];

        var now = clock.GetUtcNow();
        return await db.Bookings.Include(booking => booking.Court)
            .Where(booking => booking.VisitorTokenHash == hash && booking.Status == BookingStatus.Confirmed && booking.EndsAt > now)
            .OrderBy(booking => booking.StartsAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Booking?> FindAsync(Guid bookingId, string? visitorToken, CancellationToken cancellationToken)
    {
        if (HashOrNull(visitorToken) is not { } hash) return null;

        return await db.Bookings.Include(booking => booking.Court)
            .FirstOrDefaultAsync(booking => booking.Id == bookingId && booking.VisitorTokenHash == hash, cancellationToken)
            .ConfigureAwait(false);
    }
}
