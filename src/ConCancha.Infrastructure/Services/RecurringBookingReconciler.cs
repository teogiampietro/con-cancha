using ConCancha.Domain.Bookings;
using ConCancha.Domain.Courts;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

/// <summary>When a court's slot length or hours change, the fixed turns that no longer fit are paused and their upcoming bookings cancelled.</summary>
public sealed class RecurringBookingReconciler(
    ConCanchaDbContext db,
    CurrentClubProvider currentClub,
    RecurringBookingScheduler scheduler,
    ILogger<RecurringBookingReconciler> logger)
{
    public async Task ReconcileCourtAsync(Court court, bool slotLengthChanged, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        if (club is null) return;

        var hours = OperatingHours.For(court, club.OpeningMinuteOfDay, club.ClosingMinuteOfDay);
        var validStarts = SlotGenerator.GetStartMinutes(hours, court.SlotMinutes);
        var active = await db.RecurringBookings.Where(r => r.CourtId == court.Id && r.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var recurring in active.Where(r => slotLengthChanged || !validStarts.Contains(r.StartMinuteOfDay)))
            await PauseAsync(recurring, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Courts without their own hours follow the club's, so a change there can leave their fixed turns outside the grid.</summary>
    public async Task ReconcileClubHoursAsync(CancellationToken cancellationToken)
    {
        var courts = await db.Courts.Where(court => court.OpeningMinuteOfDay == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var court in courts)
            await ReconcileCourtAsync(court, slotLengthChanged: false, cancellationToken).ConfigureAwait(false);
    }

    private async Task PauseAsync(RecurringBooking recurring, CancellationToken cancellationToken)
    {
        recurring.IsActive = false;
        var cancelled = await scheduler.CancelUpcomingAsync(recurring.Id, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Fixed turn {RecurringBookingId} paused after a court change; {Count} upcoming bookings cancelled", recurring.Id, cancelled);
    }
}
