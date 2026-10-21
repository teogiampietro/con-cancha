using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Agenda;
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

public sealed class AgendaService(ConCanchaDbContext db, CurrentClubProvider currentClub, TimeProvider clock) : IAgendaService
{
    public async Task<AgendaDayDto> GetDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        if (club is null) return new AgendaDayDto(date, []);

        var zone = ClubTimeZone.For(club);
        var courts = await db.Courts.AsNoTracking().Where(court => court.IsActive)
            .OrderBy(court => court.Sport).ThenBy(court => court.CreatedAt).ThenBy(court => court.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var rules = await LoadRulesAsync(cancellationToken).ConfigureAwait(false);
        var bookings = await LoadBookingsAsync(zone.GetDayRange(date), cancellationToken).ConfigureAwait(false);

        var agendaCourts = courts
            .Select(court => BuildCourt(court, club, date, zone, rules[court.Id], bookings[court.Id]))
            .ToList();
        return new AgendaDayDto(date, agendaCourts);
    }

    private async Task<ILookup<Guid, PriceRule>> LoadRulesAsync(CancellationToken cancellationToken) =>
        (await db.PriceRules.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToLookup(rule => rule.CourtId);

    private async Task<ILookup<Guid, Booking>> LoadBookingsAsync(
        (DateTimeOffset Start, DateTimeOffset End) range, CancellationToken cancellationToken) =>
        (await db.Bookings.AsNoTracking()
            .Where(booking => booking.Status == BookingStatus.Confirmed
                && booking.StartsAt >= range.Start && booking.StartsAt < range.End)
            .ToListAsync(cancellationToken).ConfigureAwait(false))
        .ToLookup(booking => booking.CourtId);

    private AgendaCourtDto BuildCourt(
        Court court, Club club, DateOnly date, ClubTimeZone zone, IEnumerable<PriceRule> rules, IEnumerable<Booking> bookings)
    {
        var byStart = bookings.ToDictionary(booking => zone.ToLocal(booking.StartsAt).MinuteOfDay);
        var hours = OperatingHours.For(court, club.OpeningMinuteOfDay, club.ClosingMinuteOfDay);
        var starts = SlotGenerator.GetStartMinutes(hours, court.SlotMinutes).Union(byStart.Keys).Order();

        var slots = starts.Select(start => BuildSlot(court, date, zone, rules, start, byStart.GetValueOrDefault(start))).ToList();
        return new AgendaCourtDto(CourtMapper.ToDto(court), slots);
    }

    private AgendaSlotDto BuildSlot(
        Court court, DateOnly date, ClubTimeZone zone, IEnumerable<PriceRule> rules, int startMinute, Booking? booking)
    {
        var price = PriceResolver.Resolve(rules, date.DayOfWeek, startMinute);
        var isPast = zone.ToUtc(date, startMinute) <= clock.GetUtcNow();
        BookingDto? dto = booking is null ? null : BookingMapper.ToDto(booking, zone);
        var endMinute = dto?.EndMinuteOfDay ?? startMinute + court.SlotMinutes;
        return new AgendaSlotDto(startMinute, endMinute, price, isPast, dto);
    }
}
