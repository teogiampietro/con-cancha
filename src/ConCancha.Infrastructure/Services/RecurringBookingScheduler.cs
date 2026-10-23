using ConCancha.Domain.Bookings;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Pricing;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

public sealed record RecurringGeneration(int CreatedCount, IReadOnlyList<DateOnly> SkippedDates)
{
    public static readonly RecurringGeneration None = new(0, []);
}

/// <summary>
/// Keeps the rolling window of a club's fixed turns filled. It is idempotent: a date that already has a booking of the
/// series (even a cancelled or moved one) is left alone, and the database constraint decides who wins a contested slot.
/// </summary>
public sealed class RecurringBookingScheduler(
    ConCanchaDbContext db,
    CurrentClubProvider currentClub,
    SlotBooker slotBooker,
    TimeProvider clock,
    ILogger<RecurringBookingScheduler> logger)
{
    public async Task<RecurringGeneration> GenerateAsync(RecurringBooking recurring, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        var court = await db.Courts.FirstOrDefaultAsync(c => c.Id == recurring.CourtId, cancellationToken).ConfigureAwait(false);
        if (club is null || court is null || !court.IsActive || !recurring.IsActive) return RecurringGeneration.None;

        var zone = ClubTimeZone.For(club);
        var pending = await FindMissingDatesAsync(recurring, zone, cancellationToken).ConfigureAwait(false);
        var rules = await db.PriceRules.AsNoTracking().Where(rule => rule.CourtId == court.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var skipped = new List<DateOnly>();
        foreach (var date in pending)
        {
            var booking = NewBooking(recurring, court, zone, rules, date);
            if (booking is null || !await TrySaveAsync(booking, cancellationToken).ConfigureAwait(false)) skipped.Add(date);
        }

        return new RecurringGeneration(pending.Count - skipped.Count, skipped);
    }

    private async Task<bool> TrySaveAsync(Booking booking, CancellationToken cancellationToken)
    {
        var saved = await slotBooker.TrySaveAsync(booking, cancellationToken).ConfigureAwait(false);
        if (!saved.IsSuccess) db.Entry(booking).State = EntityState.Detached;
        return saved.IsSuccess;
    }

    public async Task RenewClubAsync(CancellationToken cancellationToken)
    {
        var active = await db.RecurringBookings.Where(recurring => recurring.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var recurring in active)
        {
            var generation = await GenerateAsync(recurring, cancellationToken).ConfigureAwait(false);
            if (generation.CreatedCount > 0)
                logger.LogInformation("Fixed turn {RecurringBookingId} renewed with {Count} bookings", recurring.Id, generation.CreatedCount);
        }
    }

    /// <summary>Marks the series' upcoming bookings as cancelled. The caller saves.</summary>
    public async Task<int> CancelUpcomingAsync(Guid recurringId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var upcoming = await db.Bookings
            .Where(booking => booking.RecurringBookingId == recurringId
                && booking.Status != BookingStatus.Cancelled && booking.StartsAt > now)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var booking in upcoming)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = now;
        }

        return upcoming.Count;
    }

    /// <summary>A fixed turn follows the court's price for its slot, so unpaid upcoming ones are re-priced when the prices change.</summary>
    public async Task RepriceUpcomingAsync(Guid courtId, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        if (club is null) return;

        var now = clock.GetUtcNow();
        var zone = ClubTimeZone.For(club);
        var rules = await db.PriceRules.AsNoTracking().Where(rule => rule.CourtId == courtId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var upcoming = await db.Bookings
            .Where(booking => booking.CourtId == courtId && booking.RecurringBookingId != null
                && booking.Status != BookingStatus.Cancelled && booking.PaymentStatus == PaymentStatus.Pending
                && booking.StartsAt > now)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var booking in upcoming)
        {
            var (date, minute) = zone.ToLocal(booking.StartsAt);
            if (PriceResolver.Resolve(rules, date.DayOfWeek, minute) is { } price) booking.Price = price;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<DateOnly>> FindMissingDatesAsync(
        RecurringBooking recurring, ClubTimeZone zone, CancellationToken cancellationToken)
    {
        var today = zone.ToLocal(clock.GetUtcNow()).Date;
        var taken = (await db.Bookings.AsNoTracking().Where(booking => booking.RecurringBookingId == recurring.Id)
                .Select(booking => booking.StartsAt).ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(startsAt => zone.ToLocal(startsAt).Date)
            .ToHashSet();
        var now = clock.GetUtcNow();
        return RecurringSchedule.OccurrencesFrom(today, recurring.Day)
            .Where(date => !taken.Contains(date) && zone.ToUtc(date, recurring.StartMinuteOfDay) > now)
            .ToList();
    }

    /// <returns><c>null</c> when no price rule covers that date and hour.</returns>
    private static Booking? NewBooking(
        RecurringBooking recurring, Court court, ClubTimeZone zone, IReadOnlyList<PriceRule> rules, DateOnly date)
    {
        if (PriceResolver.Resolve(rules, date.DayOfWeek, recurring.StartMinuteOfDay) is not { } price) return null;

        var startsAt = zone.ToUtc(date, recurring.StartMinuteOfDay);
        var endsAt = zone.ToUtc(date, recurring.StartMinuteOfDay + court.SlotMinutes);
        var target = new SlotTarget(court, startsAt, endsAt, date, recurring.StartMinuteOfDay);
        var booking = SlotBooker.NewBooking(target, BookingKind.Reservation, BookingOrigin.Panel);
        booking.RecurringBookingId = recurring.Id;
        booking.CustomerId = recurring.CustomerId;
        booking.CustomerName = recurring.CustomerName;
        booking.CustomerPhone = recurring.CustomerPhone;
        booking.Price = price;
        return booking;
    }
}
