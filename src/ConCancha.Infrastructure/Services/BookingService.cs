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

namespace ConCancha.Infrastructure.Services;

public sealed class BookingService(
    ConCanchaDbContext db,
    CurrentClubProvider currentClub,
    SlotBooker slotBooker,
    CustomerResolver customers) : IBookingService
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

    public Task<Result<BookingDto>> CancelAsync(Guid bookingId, CancellationToken cancellationToken) =>
        ChangeAsync(bookingId, Cancel, saveChanges: true, cancellationToken);

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

    private static ServiceError? Cancel(Booking booking)
    {
        if (booking.Status == BookingStatus.Cancelled) return ServiceError.Conflict(BookingMessages.AlreadyCancelled);

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTimeOffset.UtcNow;
        return null;
    }

    private static ServiceError? RequireActiveReservation(Booking booking)
    {
        if (booking.Status == BookingStatus.Cancelled) return ServiceError.Conflict(BookingMessages.CancelledCannotChange);
        return booking.Kind == BookingKind.Reservation ? null : ServiceError.Conflict(BookingMessages.NotAReservation);
    }
}
