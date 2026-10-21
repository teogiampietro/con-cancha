using ConCancha.Domain.Courts;

namespace ConCancha.Contracts.Setup;

/// <param name="NightPrice">Price from <see cref="NightFromMinuteOfDay"/> on; null when the sport has a single price.</param>
public record SportSetupDto(
    Sport Sport,
    int CourtCount,
    int SlotMinutes,
    decimal? Price,
    decimal? NightPrice,
    int? NightFromMinuteOfDay);
