using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Pricing;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConCancha.Infrastructure.Services;

public sealed record SlotTarget(Court Court, DateTimeOffset StartsAt, DateTimeOffset EndsAt, DateOnly Date, int StartMinute);

/// <summary>The part of creating a booking that the panel and the public site share: valid slot, court price, and the save that cannot overlap.</summary>
public sealed class SlotBooker(ConCanchaDbContext db, CurrentClubProvider currentClub)
{
    private const string StartField = "startMinuteOfDay";
    private const string CourtField = "courtId";

    public async Task<Result<SlotTarget>> ResolveSlotAsync(
        Guid courtId, DateOnly date, int startMinute, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        var court = await db.Courts.FirstOrDefaultAsync(c => c.Id == courtId, cancellationToken).ConfigureAwait(false);
        if (club is null || court is null)
            return Result<SlotTarget>.Failure(ServiceError.NotFound(CourtMessages.CourtNotFound));
        if (!court.IsActive)
            return Result<SlotTarget>.Failure(ServiceError.Validation(CourtField, BookingMessages.CourtInactive));

        var hours = OperatingHours.For(court, club.OpeningMinuteOfDay, club.ClosingMinuteOfDay);
        if (!SlotGenerator.GetStartMinutes(hours, court.SlotMinutes).Contains(startMinute))
            return Result<SlotTarget>.Failure(ServiceError.Validation(StartField, BookingMessages.NotASlot));

        var zone = ClubTimeZone.For(club);
        var endMinute = startMinute + court.SlotMinutes;
        return Result<SlotTarget>.Success(
            new SlotTarget(court, zone.ToUtc(date, startMinute), zone.ToUtc(date, endMinute), date, startMinute));
    }

    public async Task<decimal?> ResolveCourtPriceAsync(SlotTarget target, CancellationToken cancellationToken)
    {
        var rules = await db.PriceRules.AsNoTracking().Where(rule => rule.CourtId == target.Court.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return PriceResolver.Resolve(rules, target.Date.DayOfWeek, target.StartMinute);
    }

    public static Booking NewBooking(SlotTarget target, BookingKind kind, BookingOrigin origin) => new()
    {
        CourtId = target.Court.Id,
        StartsAt = target.StartsAt,
        EndsAt = target.EndsAt,
        Kind = kind,
        Origin = origin,
    };

    /// <summary>
    /// The exclusion constraint decides who wins when two people take the same slot at once. Under that contention
    /// PostgreSQL may also abort the loser with a deadlock; for the person it is the same answer: the slot is gone.
    /// </summary>
    public async Task<Result> TrySaveAsync(Booking booking, CancellationToken cancellationToken)
    {
        db.Bookings.Add(booking);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception exception) when (FindPostgresError(exception) is { } sqlState && IsSlotContention(sqlState))
        {
            return Result.Failure(ServiceError.Conflict(BookingMessages.SlotTaken));
        }
        catch (Exception exception) when (FindPostgresError(exception) == PostgresErrorCodes.UniqueViolation)
        {
            return Result.Failure(ServiceError.Conflict(BookingMessages.TryAgain));
        }
    }

    private static bool IsSlotContention(string sqlState) =>
        sqlState is PostgresErrorCodes.ExclusionViolation or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure;

    private static string? FindPostgresError(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException postgres) return postgres.SqlState;
            if (current.InnerException is null) return null;
        }
        return null;
    }
}
