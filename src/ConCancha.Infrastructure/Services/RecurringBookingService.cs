using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.RecurringBookings;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Pricing;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Services.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ConCancha.Infrastructure.Services;

public sealed class RecurringBookingService(
    ConCanchaDbContext db,
    SlotBooker slotBooker,
    CustomerResolver customers,
    RecurringBookingScheduler scheduler,
    TimeProvider clock,
    ILogger<RecurringBookingService> logger) : IRecurringBookingService
{
    private const string DayField = "day";
    private const string StartField = "startMinuteOfDay";

    public async Task<Result<IReadOnlyList<RecurringBookingDto>>> ListByCourtAsync(Guid courtId, CancellationToken cancellationToken)
    {
        if (!await db.Courts.AnyAsync(court => court.Id == courtId, cancellationToken).ConfigureAwait(false))
            return Result<IReadOnlyList<RecurringBookingDto>>.Failure(ServiceError.NotFound(CourtMessages.CourtNotFound));

        var items = await db.RecurringBookings.AsNoTracking().Where(r => r.CourtId == courtId)
            .OrderBy(r => r.Day).ThenBy(r => r.StartMinuteOfDay)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<RecurringBookingDto>>.Success(items.Select(RecurringBookingMapper.ToDto).ToList());
    }

    public async Task<Result<RecurringBookingResultDto>> CreateAsync(
        Guid courtId, RecurringBookingRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Day))
            return Result<RecurringBookingResultDto>.Failure(ServiceError.Validation(DayField, RecurringBookingMessages.InvalidDay));

        var invalid = await ValidateSlotAsync(courtId, request, cancellationToken).ConfigureAwait(false);
        if (invalid is not null) return Result<RecurringBookingResultDto>.Failure(invalid);

        var recurring = await NewRecurringAsync(courtId, request, cancellationToken).ConfigureAwait(false);
        var saved = await TrySaveNewAsync(recurring, cancellationToken).ConfigureAwait(false);
        if (!saved.IsSuccess) return Result<RecurringBookingResultDto>.Failure(saved.Error!);

        return Result<RecurringBookingResultDto>.Success(await GenerateAsync(recurring, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<RecurringBookingResultDto>> UpdateAsync(
        Guid courtId, Guid recurringId, UpdateRecurringBookingRequest request, CancellationToken cancellationToken)
    {
        var recurring = await FindAsync(courtId, recurringId, cancellationToken).ConfigureAwait(false);
        if (recurring is null)
            return Result<RecurringBookingResultDto>.Failure(ServiceError.NotFound(RecurringBookingMessages.NotFound));

        recurring.CustomerName = request.CustomerName.Trim();
        recurring.CustomerPhone = NormalizePhone(request.CustomerPhone);
        recurring.CustomerId = await customers
            .FindOrCreateAsync(recurring.CustomerName, recurring.CustomerPhone, email: null, cancellationToken).ConfigureAwait(false);
        await ApplyCustomerToUpcomingAsync(recurring, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<RecurringBookingResultDto>.Success(await GenerateAsync(recurring, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result> DeleteAsync(Guid courtId, Guid recurringId, CancellationToken cancellationToken)
    {
        var recurring = await FindAsync(courtId, recurringId, cancellationToken).ConfigureAwait(false);
        if (recurring is null) return Result.Failure(ServiceError.NotFound(RecurringBookingMessages.NotFound));

        var cancelled = await scheduler.CancelUpcomingAsync(recurring.Id, cancellationToken).ConfigureAwait(false);
        db.RecurringBookings.Remove(recurring);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Fixed turn {RecurringBookingId} removed; {Count} upcoming bookings cancelled", recurringId, cancelled);
        return Result.Success();
    }

    private async Task<ServiceError?> ValidateSlotAsync(Guid courtId, RecurringBookingRequest request, CancellationToken cancellationToken)
    {
        var anyDate = RecurringSchedule.OccurrencesFrom(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), request.Day)[0];
        var target = await slotBooker.ResolveSlotAsync(courtId, anyDate, request.StartMinuteOfDay, cancellationToken).ConfigureAwait(false);
        if (target.Error is not null) return target.Error;

        var rules = await db.PriceRules.AsNoTracking().Where(rule => rule.CourtId == courtId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (PriceResolver.Resolve(rules, request.Day, request.StartMinuteOfDay) is null)
            return ServiceError.Validation(StartField, RecurringBookingMessages.PriceMissing);

        var taken = await db.RecurringBookings.AnyAsync(
            r => r.CourtId == courtId && r.Day == request.Day && r.StartMinuteOfDay == request.StartMinuteOfDay && r.IsActive,
            cancellationToken).ConfigureAwait(false);
        return taken ? ServiceError.Conflict(RecurringBookingMessages.SlotAlreadyFixed) : null;
    }

    private async Task<RecurringBooking> NewRecurringAsync(Guid courtId, RecurringBookingRequest request, CancellationToken cancellationToken)
    {
        var recurring = new RecurringBooking
        {
            CourtId = courtId,
            Day = request.Day,
            StartMinuteOfDay = request.StartMinuteOfDay,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = NormalizePhone(request.CustomerPhone),
        };
        recurring.CustomerId = await customers
            .FindOrCreateAsync(recurring.CustomerName, recurring.CustomerPhone, email: null, cancellationToken).ConfigureAwait(false);
        return recurring;
    }

    private async Task<Result> TrySaveNewAsync(RecurringBooking recurring, CancellationToken cancellationToken)
    {
        db.RecurringBookings.Add(recurring);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Result.Failure(ServiceError.Conflict(RecurringBookingMessages.SlotAlreadyFixed));
        }
    }

    private async Task<RecurringBookingResultDto> GenerateAsync(RecurringBooking recurring, CancellationToken cancellationToken)
    {
        var generation = await scheduler.GenerateAsync(recurring, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Fixed turn {RecurringBookingId} generated {Created} bookings, {Skipped} dates skipped",
            recurring.Id, generation.CreatedCount, generation.SkippedDates.Count);
        return new RecurringBookingResultDto(RecurringBookingMapper.ToDto(recurring), generation.CreatedCount, generation.SkippedDates);
    }

    private async Task ApplyCustomerToUpcomingAsync(RecurringBooking recurring, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var upcoming = await db.Bookings
            .Where(booking => booking.RecurringBookingId == recurring.Id && booking.Status != BookingStatus.Cancelled && booking.StartsAt > now)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var booking in upcoming)
        {
            booking.CustomerName = recurring.CustomerName;
            booking.CustomerPhone = recurring.CustomerPhone;
            booking.CustomerId = recurring.CustomerId;
        }
    }

    private Task<RecurringBooking?> FindAsync(Guid courtId, Guid recurringId, CancellationToken cancellationToken) =>
        db.RecurringBookings.FirstOrDefaultAsync(r => r.Id == recurringId && r.CourtId == courtId, cancellationToken);

    private static string? NormalizePhone(string? phone) => string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
}
