using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Cash;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Cash;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

public sealed class CashService(ConCanchaDbContext db, CurrentClubProvider currentClub, TimeProvider clock) : ICashService
{
    public Task<CashDayDto> GetDayAsync(DateOnly date, CancellationToken cancellationToken) =>
        BuildAsync(date, cancellationToken);

    public async Task<CashDayDto> CloseDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var day = await BuildAsync(date, cancellationToken).ConfigureAwait(false);
        var closure = await db.CashClosures.FirstOrDefaultAsync(c => c.Date == date, cancellationToken).ConfigureAwait(false);
        if (closure is null) db.CashClosures.Add(closure = new CashClosure { Date = date });

        closure.Total = day.Total;
        closure.ClosedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return day with { Closure = new CashClosureDto(closure.ClosedAt, closure.Total) };
    }

    public async Task<CashDayDto> ReopenDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        await db.CashClosures.Where(closure => closure.Date == date).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        return await BuildAsync(date, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CashDayDto> BuildAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        if (club is null) return new CashDayDto(date, 0, 0, 0, 0, [], null);

        var zone = ClubTimeZone.For(club);
        var (start, end) = zone.GetDayRange(date);
        var bookings = await db.Bookings.AsNoTracking().Include(booking => booking.Court)
            .Where(booking => booking.Kind == BookingKind.Reservation && booking.Status == BookingStatus.Confirmed
                && booking.StartsAt >= start && booking.StartsAt < end)
            .OrderBy(booking => booking.StartsAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var closure = await db.CashClosures.AsNoTracking().FirstOrDefaultAsync(c => c.Date == date, cancellationToken).ConfigureAwait(false);

        return Summarize(date, bookings, zone, closure);
    }

    private static CashDayDto Summarize(DateOnly date, IReadOnlyList<Booking> bookings, ClubTimeZone zone, CashClosure? closure)
    {
        var cash = bookings.Where(b => b.PaymentMethod == PaymentMethod.Cash).Sum(BookingAmounts.Collected);
        var transfer = bookings.Where(b => b.PaymentMethod == PaymentMethod.Transfer).Sum(BookingAmounts.Collected);
        var lines = bookings.Select(b => new CashBookingDto(
            zone.ToLocal(b.StartsAt).MinuteOfDay, b.CustomerName ?? string.Empty, b.Court!.Name, b.Price,
            b.PaymentStatus, b.PaymentMethod, b.Attendance)).ToList();

        return new CashDayDto(
            date, cash, transfer, cash + transfer, bookings.Sum(BookingAmounts.ToCollect), lines,
            closure is null ? null : new CashClosureDto(closure.ClosedAt, closure.Total));
    }
}
