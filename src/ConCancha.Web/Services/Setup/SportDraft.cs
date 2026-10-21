using ConCancha.Domain.Courts;
using ConCancha.Domain.Setup;

namespace ConCancha.Web.Services.Setup;

public sealed class SportDraft(Sport sport)
{
    public Sport Sport { get; } = sport;
    public int CourtCount { get; set; } = 1;
    public int SlotMinutes { get; set; } = SportDefaults.GetSlotMinutes(sport);
    public decimal? Price { get; set; }
    public bool HasNightPrice { get; set; }
    public decimal? NightPrice { get; set; }

    public string Label => SportLabels.Get(Sport);

    public void ChangeCourtCount(int delta) =>
        CourtCount = Math.Clamp(CourtCount + delta, 1, CourtLimits.MaxCourtsPerSport);
}
