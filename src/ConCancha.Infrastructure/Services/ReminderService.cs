using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Reminders;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Customers;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

public sealed class ReminderService(ConCanchaDbContext db, CurrentClubProvider currentClub, TimeProvider clock) : IReminderService
{
    public async Task<ReminderListDto> ListAsync(ReminderDay day, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        if (club is null) return new ReminderListDto(string.Empty, []);

        var zone = ClubTimeZone.For(club);
        var date = zone.ToLocal(clock.GetUtcNow()).Date.AddDays(day == ReminderDay.Tomorrow ? 1 : 0);
        var (start, end) = zone.GetDayRange(date);
        var bookings = await db.Bookings.AsNoTracking().Include(booking => booking.Court)
            .Where(booking => booking.Kind == BookingKind.Reservation && booking.Status == BookingStatus.Confirmed
                && booking.Attendance == Attendance.Pending && booking.StartsAt >= start && booking.StartsAt < end)
            .OrderBy(booking => booking.StartsAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new ReminderListDto(club.Name, bookings.Select(booking => ToReminder(booking, zone)).OfType<ReminderDto>().ToList());
    }

    public async Task<Result> MarkSentAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken).ConfigureAwait(false);
        if (booking is null) return Result.Failure(ServiceError.NotFound(BookingMessages.BookingNotFound));

        booking.ReminderSentAt = clock.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>Only turns with a phone WhatsApp can reach get a reminder.</summary>
    private static ReminderDto? ToReminder(Booking booking, ClubTimeZone zone)
    {
        if (PhoneNormalizer.Normalize(booking.CustomerPhone) is not { } phone) return null;

        var (date, startMinute) = zone.ToLocal(booking.StartsAt);
        return new ReminderDto(
            booking.Id, booking.CustomerName ?? string.Empty, phone, booking.Court!.Name, date, startMinute, booking.ReminderSentAt is not null);
    }
}
