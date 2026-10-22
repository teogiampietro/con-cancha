using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Bookings;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Pricing;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Services.Mapping;
using ConCancha.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

public sealed class BookingService(
    ConCanchaDbContext db,
    CurrentClubProvider currentClub,
    SlotBooker slotBooker,
    CustomerResolver customers,
    TimeProvider clock,
    ILogger<BookingService> logger) : IBookingService
{
    private const string PriceField = "price";
    private const string StartField = "startMinuteOfDay";
    private const string CourtField = "courtId";
    private const string PaymentMethodField = "paymentMethod";

    public async Task<Result<BookingDto>> CreateReservationAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        if (ValidatePayment(request) is { } invalidPayment) return Result<BookingDto>.Failure(invalidPayment);

        var target = await slotBooker.ResolveSlotAsync(request.CourtId, request.Date, request.StartMinuteOfDay, cancellationToken).ConfigureAwait(false);
        if (target.Error is not null) return Result<BookingDto>.Failure(target.Error);

        var price = await ResolvePriceAsync(request.Price, target.Value!, cancellationToken).ConfigureAwait(false);
        if (price.Error is not null) return Result<BookingDto>.Failure(price.Error);

        var booking = await NewReservationAsync(target.Value!, request, price.Value, cancellationToken).ConfigureAwait(false);
        return await SaveNewAsync(booking, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<BookingDto>> CreateBlockAsync(CreateBlockRequest request, CancellationToken cancellationToken)
    {
        var target = await slotBooker.ResolveSlotAsync(request.CourtId, request.Date, request.StartMinuteOfDay, cancellationToken).ConfigureAwait(false);
        if (target.Error is not null) return Result<BookingDto>.Failure(target.Error);

        var booking = SlotBooker.NewBooking(target.Value!, BookingKind.Block, BookingOrigin.Panel);
        booking.Reason = request.Reason.Trim();
        return await SaveNewAsync(booking, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<BookingDto>> GetAsync(Guid bookingId, CancellationToken cancellationToken) =>
        await ChangeAsync(bookingId, _ => null, saveChanges: false, cancellationToken).ConfigureAwait(false);

    public Task<Result<BookingDto>> MarkPaidAsync(Guid bookingId, MarkPaidRequest request, CancellationToken cancellationToken) =>
        ChangeAsync(bookingId, booking => MarkPaid(booking, request.Method), saveChanges: true, cancellationToken);

    public Task<Result<BookingDto>> MarkPlayedAsync(Guid bookingId, CancellationToken cancellationToken) =>
        ChangeAsync(bookingId, booking => SetAttendance(booking, Attendance.Played), saveChanges: true, cancellationToken);

    public Task<Result<BookingDto>> MarkNoShowAsync(Guid bookingId, CancellationToken cancellationToken) =>
        ChangeAsync(bookingId, MarkNoShow, saveChanges: true, cancellationToken);

    public Task<Result<BookingDto>> ChangePriceAsync(Guid bookingId, ChangePriceRequest request, CancellationToken cancellationToken) =>
        ChangeAsync(bookingId, booking => ChangePrice(booking, request.Price), saveChanges: true, cancellationToken);

    public Task<Result<BookingDto>> MarkUnpaidAsync(Guid bookingId, CancellationToken cancellationToken) =>
        ChangeAsync(bookingId, booking => ApplyRule(booking, b => b.MarkUnpaid()), saveChanges: true, cancellationToken);

    public Task<Result<BookingDto>> ResetAttendanceAsync(Guid bookingId, CancellationToken cancellationToken) =>
        ChangeAsync(bookingId, booking => ApplyRule(booking, b => b.UndoAttendance()), saveChanges: true, cancellationToken);

    public Task<Result<BookingDto>> ChangePaymentMethodAsync(Guid bookingId, MarkPaidRequest request, CancellationToken cancellationToken) =>
        ChangeAsync(bookingId, booking => ApplyRule(booking, b => b.ChangePaymentMethod(request.Method)), saveChanges: true, cancellationToken);

    public async Task<Result<BookingDto>> MoveAsync(Guid bookingId, MoveBookingRequest request, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken).ConfigureAwait(false);
        if (club is null || booking is null)
            return Result<BookingDto>.Failure(ServiceError.NotFound(BookingMessages.BookingNotFound));

        var zone = ClubTimeZone.For(club);
        var moved = await TryMoveAsync(booking, request, zone, cancellationToken).ConfigureAwait(false);
        if (moved.Error is not null) return Result<BookingDto>.Failure(moved.Error);

        logger.LogInformation("Booking {BookingId} moved to court {CourtId} for club {ClubId}", booking.Id, booking.CourtId, booking.ClubId);
        return Result<BookingDto>.Success(BookingMapper.ToDto(booking, zone));
    }

    private async Task<Result> TryMoveAsync(
        Booking booking, MoveBookingRequest request, ClubTimeZone zone, CancellationToken cancellationToken)
    {
        var (date, _) = zone.ToLocal(booking.StartsAt);
        var target = await slotBooker.ResolveSlotAsync(request.CourtId, date, request.StartMinuteOfDay, cancellationToken).ConfigureAwait(false);
        if (target.Error is not null) return Result.Failure(target.Error);
        if (target.Value!.StartsAt <= clock.GetUtcNow()) return Result.Failure(ServiceError.Conflict(BookingMessages.SlotInThePast));

        var slot = target.Value!;
        if (ApplyRule(booking, b => b.Move(slot.Court.Id, slot.StartsAt, slot.EndsAt)) is { } broken)
            return Result.Failure(broken);

        return await slotBooker.TrySaveChangesAsync(booking, cancellationToken).ConfigureAwait(false);
    }

    private static ServiceError? ApplyRule(Booking booking, Action<Booking> rule)
    {
        try
        {
            rule(booking);
            return null;
        }
        catch (BookingRuleException exception)
        {
            return BookingRuleErrors.ToServiceError(exception.Rule);
        }
    }

    public Task<Result<BookingDto>> CancelAsync(Guid bookingId, CancellationToken cancellationToken) =>
        ChangeAsync(bookingId, CancelAndLog, saveChanges: true, cancellationToken);

    private async Task<Result<BookingDto>> ChangeAsync(
        Guid bookingId, Func<Booking, ServiceError?> apply, bool saveChanges, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken).ConfigureAwait(false);
        if (club is null || booking is null)
            return Result<BookingDto>.Failure(ServiceError.NotFound(BookingMessages.BookingNotFound));

        if (apply(booking) is { } error) return Result<BookingDto>.Failure(error);

        if (saveChanges) await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<BookingDto>.Success(BookingMapper.ToDto(booking, ClubTimeZone.For(club)));
    }

    private async Task<Result<decimal>> ResolvePriceAsync(decimal? requested, SlotTarget target, CancellationToken cancellationToken)
    {
        var price = requested ?? await slotBooker.ResolveCourtPriceAsync(target, cancellationToken).ConfigureAwait(false);
        if (price is null)
            return Result<decimal>.Failure(ServiceError.Validation(PriceField, BookingMessages.PriceRequired));

        return PriceRuleValidator.ValidatePrice(price.Value) is { } reason
            ? Result<decimal>.Failure(ServiceError.Validation(PriceField, reason))
            : Result<decimal>.Success(price.Value);
    }

    private async Task<Booking> NewReservationAsync(
        SlotTarget target, CreateBookingRequest request, decimal price, CancellationToken cancellationToken)
    {
        var booking = SlotBooker.NewBooking(target, BookingKind.Reservation, BookingOrigin.Panel);
        booking.CustomerName = request.CustomerName.Trim();
        booking.CustomerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? null : request.CustomerPhone.Trim();
        booking.CustomerId = await customers
            .FindOrCreateAsync(booking.CustomerName, booking.CustomerPhone, email: null, cancellationToken).ConfigureAwait(false);
        booking.Price = price;
        ApplyPayment(booking, request.IsPaid, request.PaymentMethod);
        return booking;
    }

    private async Task<Result<BookingDto>> SaveNewAsync(Booking booking, CancellationToken cancellationToken)
    {
        var club = (await currentClub.GetAsync(cancellationToken).ConfigureAwait(false))!;
        var saved = await slotBooker.TrySaveAsync(booking, cancellationToken).ConfigureAwait(false);

        return saved.IsSuccess
            ? Result<BookingDto>.Success(BookingMapper.ToDto(booking, ClubTimeZone.For(club)))
            : Result<BookingDto>.Failure(saved.Error!);
    }

    private static ServiceError? ValidatePayment(CreateBookingRequest request) =>
        request.IsPaid && request.PaymentMethod is null
            ? ServiceError.Validation(PaymentMethodField, BookingMessages.PaymentMethodRequired)
            : null;

    private static void ApplyPayment(Booking booking, bool isPaid, PaymentMethod? method)
    {
        if (!isPaid) return;

        booking.PaymentStatus = PaymentStatus.Paid;
        booking.PaymentMethod = method;
    }

    private static ServiceError? MarkPaid(Booking booking, PaymentMethod method)
    {
        if (RequireActiveReservation(booking) is { } error) return error;
        if (booking.PaymentStatus == PaymentStatus.Paid) return ServiceError.Conflict(BookingMessages.AlreadyPaid);
        if (!Enum.IsDefined(method)) return ServiceError.Validation(PaymentMethodField, BookingMessages.PaymentMethodRequired);

        booking.PaymentStatus = PaymentStatus.Paid;
        booking.PaymentMethod = method;
        return null;
    }

    private static ServiceError? SetAttendance(Booking booking, Attendance attendance)
    {
        if (RequireActiveReservation(booking) is { } error) return error;

        booking.Attendance = attendance;
        return null;
    }

    private static ServiceError? MarkNoShow(Booking booking)
    {
        if (booking.PaymentStatus == PaymentStatus.Paid && booking.Kind == BookingKind.Reservation)
            return ServiceError.Conflict(BookingMessages.PaidCannotBeNoShow);

        return SetAttendance(booking, Attendance.NoShow);
    }

    private static ServiceError? ChangePrice(Booking booking, decimal price)
    {
        if (RequireActiveReservation(booking) is { } error) return error;
        if (PriceRuleValidator.ValidatePrice(price) is { } reason) return ServiceError.Validation(PriceField, reason);

        booking.Price = price;
        return null;
    }

    private ServiceError? CancelAndLog(Booking booking)
    {
        if (booking.Status == BookingStatus.Cancelled) return ServiceError.Conflict(BookingMessages.AlreadyCancelled);

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTimeOffset.UtcNow;
        logger.LogInformation("Booking {BookingId} cancelled from the panel for club {ClubId}", booking.Id, booking.ClubId);
        return null;
    }

    private static ServiceError? RequireActiveReservation(Booking booking)
    {
        if (booking.Status == BookingStatus.Cancelled) return ServiceError.Conflict(BookingMessages.CancelledCannotChange);
        return booking.Kind == BookingKind.Reservation ? null : ServiceError.Conflict(BookingMessages.NotAReservation);
    }
}
