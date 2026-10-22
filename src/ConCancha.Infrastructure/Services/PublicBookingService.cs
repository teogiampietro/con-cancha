using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Agenda;
using ConCancha.Contracts.Public;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Customers;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

public sealed class PublicBookingService(
    ConCanchaDbContext db,
    ITenantContext tenant,
    IAgendaService agenda,
    SlotBooker slotBooker,
    CustomerResolver customers,
    VisitorBookings visitors,
    TimeProvider clock,
    ILogger<PublicBookingService> logger) : IPublicBookingService
{
    private const string PhoneField = "phone";
    private const string DateField = "date";

    public async Task<Result<PublicDayDto>> GetDayAsync(
        string slug, DateOnly date, string? visitorToken, CancellationToken cancellationToken)
    {
        var club = await EnterClubAsync(slug, cancellationToken).ConfigureAwait(false);
        if (club is null) return Result<PublicDayDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));
        if (!IsWithinBookableDays(club, date)) return Result<PublicDayDto>.Failure(ServiceError.Validation(DateField, PublicBookingMessages.DateOutOfRange));

        var day = await agenda.GetDayAsync(date, cancellationToken).ConfigureAwait(false);
        var mine = await visitors.GetIdsOnDayAsync(club, date, visitorToken, cancellationToken).ConfigureAwait(false);
        return Result<PublicDayDto>.Success(ToPublicDay(day, mine));
    }

    public async Task<Result<PublicBookingCreatedDto>> BookAsync(
        string slug, PublicBookingRequest request, string? visitorToken, CancellationToken cancellationToken)
    {
        var club = await EnterClubAsync(slug, cancellationToken).ConfigureAwait(false);
        if (club is null) return Result<PublicBookingCreatedDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));
        if (Validate(club, request) is { } invalid) return Result<PublicBookingCreatedDto>.Failure(invalid);

        var prepared = await PrepareBookingAsync(request, cancellationToken).ConfigureAwait(false);
        if (prepared.Error is not null) return Result<PublicBookingCreatedDto>.Failure(prepared.Error);

        var (booking, token) = prepared.Value;
        booking!.VisitorTokenHash = VisitorBookings.HashOrNull(visitorToken);
        var saved = await slotBooker.TrySaveAsync(booking, cancellationToken).ConfigureAwait(false);
        if (!saved.IsSuccess) return Result<PublicBookingCreatedDto>.Failure(saved.Error!);

        return Result<PublicBookingCreatedDto>.Success(new PublicBookingCreatedDto(token!, ToDto(club, booking, request.CourtId)));
    }

    public async Task<Result<PublicBookingDto>> GetBookingAsync(string slug, string token, CancellationToken cancellationToken)
    {
        var club = await EnterClubAsync(slug, cancellationToken).ConfigureAwait(false);
        var booking = club is null ? null : await FindByTokenAsync(token, cancellationToken).ConfigureAwait(false);

        return booking is null
            ? Result<PublicBookingDto>.Failure(ServiceError.NotFound(PublicBookingMessages.BookingNotFound))
            : Result<PublicBookingDto>.Success(ToDto(club!, booking, booking.CourtId));
    }

    public async Task<Result<PublicBookingDto>> CancelAsync(string slug, string token, CancellationToken cancellationToken)
    {
        var club = await EnterClubAsync(slug, cancellationToken).ConfigureAwait(false);
        var booking = club is null ? null : await FindByTokenAsync(token, cancellationToken).ConfigureAwait(false);
        if (booking is null) return Result<PublicBookingDto>.Failure(ServiceError.NotFound(PublicBookingMessages.BookingNotFound));
        return await CancelOnlineAsync(club!, booking, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<PublicMyBookingsDto>> GetMyBookingsAsync(
        string slug, string? visitorToken, CancellationToken cancellationToken)
    {
        var club = await EnterClubAsync(slug, cancellationToken).ConfigureAwait(false);
        if (club is null) return Result<PublicMyBookingsDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        return Result<PublicMyBookingsDto>.Success(await BuildMyBookingsAsync(club, visitorToken, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<PublicMyBookingsDto>> CancelMineAsync(
        string slug, Guid bookingId, string? visitorToken, CancellationToken cancellationToken)
    {
        var club = await EnterClubAsync(slug, cancellationToken).ConfigureAwait(false);
        var booking = club is null ? null : await visitors.FindAsync(bookingId, visitorToken, cancellationToken).ConfigureAwait(false);
        if (booking is null) return Result<PublicMyBookingsDto>.Failure(ServiceError.NotFound(PublicBookingMessages.BookingNotFound));

        var cancelled = await CancelOnlineAsync(club!, booking, cancellationToken).ConfigureAwait(false);
        if (!cancelled.IsSuccess) return Result<PublicMyBookingsDto>.Failure(cancelled.Error!);

        return Result<PublicMyBookingsDto>.Success(await BuildMyBookingsAsync(club!, visitorToken, cancellationToken).ConfigureAwait(false));
    }

    private async Task<Result<PublicBookingDto>> CancelOnlineAsync(Club club, Booking booking, CancellationToken cancellationToken)
    {
        if (RejectCancellation(club, booking) is { } error) return Result<PublicBookingDto>.Failure(error);

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = clock.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Booking {BookingId} cancelled online for club {ClubId}", booking.Id, club.Id);
        return Result<PublicBookingDto>.Success(ToDto(club, booking, booking.CourtId));
    }

    private async Task<PublicMyBookingsDto> BuildMyBookingsAsync(Club club, string? visitorToken, CancellationToken cancellationToken)
    {
        var bookings = await visitors.ListUpcomingAsync(visitorToken, cancellationToken).ConfigureAwait(false);
        return new PublicMyBookingsDto(
            club.Name, club.Phone, club.CancellationWindowHours, bookings.Select(booking => ToMyBookingDto(club, booking)).ToList());
    }

    private PublicMyBookingDto ToMyBookingDto(Club club, Booking booking)
    {
        var (date, startMinute) = ClubTimeZone.For(club).ToLocal(booking.StartsAt);
        var cancelableUntil = booking.StartsAt - TimeSpan.FromHours(club.CancellationWindowHours);
        return new PublicMyBookingDto(
            booking.Id, booking.Court!.Name, date, startMinute, startMinute + (int)(booking.EndsAt - booking.StartsAt).TotalMinutes,
            booking.Price, clock.GetUtcNow() <= cancelableUntil, cancelableUntil);
    }

    /// <summary>Resolves the club from the link and scopes every later query to it.</summary>
    private async Task<Club?> EnterClubAsync(string slug, CancellationToken cancellationToken)
    {
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Slug == slug.ToLowerInvariant(), cancellationToken).ConfigureAwait(false);
        if (club is not null) tenant.SetClub(club.Id);
        return club;
    }

    private async Task<Result<(Booking? Booking, string? Token)>> PrepareBookingAsync(
        PublicBookingRequest request, CancellationToken cancellationToken)
    {
        var target = await slotBooker.ResolveSlotAsync(request.CourtId, request.Date, request.StartMinuteOfDay, cancellationToken).ConfigureAwait(false);
        if (target.Error is not null) return Failure(target.Error);
        if (target.Value!.StartsAt <= clock.GetUtcNow()) return Failure(ServiceError.Validation(DateField, PublicBookingMessages.SlotAlreadyPassed));

        var price = await slotBooker.ResolveCourtPriceAsync(target.Value, cancellationToken).ConfigureAwait(false);
        if (price is null) return Failure(ServiceError.Conflict(PublicBookingMessages.SlotWithoutPrice));

        var phone = request.Phone.Trim();
        var customerId = await customers.FindOrCreateAsync(request.Name.Trim(), phone, NormalizeEmail(request.Email), cancellationToken).ConfigureAwait(false);
        if (await HasTooManyActiveBookingsAsync(customerId!.Value, cancellationToken).ConfigureAwait(false))
            return Failure(ServiceError.Conflict(string.Format(PublicBookingMessages.TooManyActiveBookings, BookingPolicy.MaxActiveBookingsPerPhone)));

        var token = ManagementToken.Generate();
        var booking = SlotBooker.NewBooking(target.Value, BookingKind.Reservation, BookingOrigin.Online);
        booking.CustomerId = customerId;
        booking.CustomerName = request.Name.Trim();
        booking.CustomerPhone = phone;
        booking.Price = price.Value;
        booking.ManagementTokenHash = ManagementToken.Hash(token);
        return Result<(Booking?, string?)>.Success((booking, token));
    }

    private static Result<(Booking? Booking, string? Token)> Failure(ServiceError error) =>
        Result<(Booking?, string?)>.Failure(error);

    private async Task<bool> HasTooManyActiveBookingsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var active = await db.Bookings.CountAsync(
            booking => booking.CustomerId == customerId && booking.Kind == BookingKind.Reservation
                && booking.Status == BookingStatus.Confirmed && booking.StartsAt > now,
            cancellationToken).ConfigureAwait(false);
        return active >= BookingPolicy.MaxActiveBookingsPerPhone;
    }

    private Task<Booking?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        var hash = ManagementToken.Hash(token);
        return db.Bookings.Include(booking => booking.Court)
            .FirstOrDefaultAsync(booking => booking.ManagementTokenHash == hash, cancellationToken);
    }

    private ServiceError? Validate(Club club, PublicBookingRequest request)
    {
        if (PhoneNormalizer.Normalize(request.Phone) is null)
            return ServiceError.Validation(PhoneField, PublicBookingMessages.InvalidPhone);

        return IsWithinBookableDays(club, request.Date)
            ? null
            : ServiceError.Validation(DateField, PublicBookingMessages.DateOutOfRange);
    }

    private bool IsWithinBookableDays(Club club, DateOnly date)
    {
        var today = ClubTimeZone.For(club).ToLocal(clock.GetUtcNow()).Date;
        return date >= today && date < today.AddDays(BookingPolicy.PublicBookingDays);
    }

    private ServiceError? RejectCancellation(Club club, Booking booking)
    {
        if (booking.Status == BookingStatus.Cancelled) return ServiceError.Conflict(PublicBookingMessages.AlreadyCancelled);

        return BookingPolicy.CanCancelOnline(booking.StartsAt, clock.GetUtcNow(), club.CancellationWindowHours)
            ? null
            : ServiceError.Conflict(string.Format(PublicBookingMessages.CancellationWindowClosed, club.CancellationWindowHours));
    }

    private PublicBookingDto ToDto(Club club, Booking booking, Guid courtId)
    {
        var zone = ClubTimeZone.For(club);
        var (date, startMinute) = zone.ToLocal(booking.StartsAt);
        var cancelableUntil = booking.StartsAt - TimeSpan.FromHours(club.CancellationWindowHours);
        var isOpen = booking.Status == BookingStatus.Confirmed;
        var courtName = booking.Court?.Name ?? db.Courts.Where(court => court.Id == courtId).Select(court => court.Name).First();

        return new PublicBookingDto(
            club.Name, club.Phone, courtName, date, startMinute,
            startMinute + (int)(booking.EndsAt - booking.StartsAt).TotalMinutes, booking.Price, booking.Status,
            isOpen && clock.GetUtcNow() <= cancelableUntil, isOpen ? cancelableUntil : null, club.CancellationWindowHours);
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static PublicDayDto ToPublicDay(AgendaDayDto day, IReadOnlySet<Guid> mine) => new(
        day.Date,
        day.Courts.Select(courtDay => new PublicCourtDto(
            courtDay.Court.Id, courtDay.Court.Name, courtDay.Court.Sport, courtDay.Court.SlotMinutes,
            courtDay.Slots.Select(slot => ToPublicSlot(slot, mine)).ToList())).ToList());

    private static PublicSlotDto ToPublicSlot(AgendaSlotDto slot, IReadOnlySet<Guid> mine) => new(
        slot.StartMinuteOfDay, slot.EndMinuteOfDay, slot.Price, ToSlotState(slot, mine));

    private static PublicSlotState ToSlotState(AgendaSlotDto slot, IReadOnlySet<Guid> mine)
    {
        if (slot.Booking is null) return slot.IsPast ? PublicSlotState.Past : PublicSlotState.Free;
        if (slot.Booking.Kind == BookingKind.Block) return PublicSlotState.Blocked;
        return mine.Contains(slot.Booking.Id) ? PublicSlotState.Mine : PublicSlotState.Taken;
    }
}
