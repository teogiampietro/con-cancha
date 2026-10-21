using ConCancha.Domain.Clubs;
using ConCancha.Domain.Courts;

namespace ConCancha.Domain.Pricing;

public class PriceRule : IBelongsToClub
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }

    public Guid CourtId { get; set; }
    public Court? Court { get; set; }

    /// <summary>Null means every day of the week.</summary>
    public Weekdays? Days { get; set; }

    public int FromMinuteOfDay { get; set; }
    public int ToMinuteOfDay { get; set; }
    public decimal Price { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool AppliesTo(Weekdays day, int minuteOfDay) =>
        (Days is null || Days.Value.HasFlag(day)) && minuteOfDay >= FromMinuteOfDay && minuteOfDay < ToMinuteOfDay;
}
