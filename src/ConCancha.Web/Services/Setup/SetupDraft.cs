using ConCancha.Contracts.Setup;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Pricing;
using ConCancha.Domain.Setup;

namespace ConCancha.Web.Services.Setup;

/// <summary>Answers of the setup wizard while they are being typed. Pure state: no UI and no API calls.</summary>
public sealed class SetupDraft
{
    private readonly List<SportDraft> _sports = [];

    public IReadOnlyList<SportDraft> Sports => _sports;
    public string OpeningText { get; set; } = DisplayFormat.Time(ClubDefaults.OpeningMinuteOfDay);
    public string ClosingText { get; set; } = DisplayFormat.Time(ClubDefaults.ClosingMinuteOfDay);

    public bool IsSelected(Sport sport) => _sports.Any(draft => draft.Sport == sport);

    public void ToggleSport(Sport sport)
    {
        if (_sports.RemoveAll(draft => draft.Sport == sport) > 0) return;

        _sports.Add(new SportDraft(sport));
        _sports.Sort((left, right) => left.Sport.CompareTo(right.Sport));
    }

    public IReadOnlyList<SetupStep> BuildSteps() =>
    [
        new(SetupStepKind.Sports),
        .. _sports.Select(sport => new SetupStep(SetupStepKind.Courts, sport)),
        new(SetupStepKind.Hours),
        .. _sports.Select(sport => new SetupStep(SetupStepKind.Pricing, sport)),
        new(SetupStepKind.Summary),
    ];

    public SetupAnswersDto ToDto()
    {
        var hours = TryGetHours();
        return new SetupAnswersDto(
            _sports.Select(ToDto).ToList(),
            hours?.OpeningMinuteOfDay ?? ClubDefaults.OpeningMinuteOfDay,
            hours?.ClosingMinuteOfDay ?? ClubDefaults.ClosingMinuteOfDay);
    }

    public static SetupDraft From(SetupAnswersDto answers)
    {
        var draft = new SetupDraft
        {
            OpeningText = DisplayFormat.Time(answers.OpeningMinuteOfDay),
            ClosingText = DisplayFormat.Time(answers.ClosingMinuteOfDay),
        };
        foreach (var saved in answers.Sports) draft._sports.Add(FromDto(saved));
        return draft;
    }

    /// <summary>Why the step cannot be left yet, or <c>null</c> when it is complete.</summary>
    public string? Validate(SetupStep step) => step.Kind switch
    {
        SetupStepKind.Sports => _sports.Count == 0 ? "Elegí al menos un deporte." : null,
        SetupStepKind.Hours => ValidateHours(),
        SetupStepKind.Pricing => ValidatePricing(step.Sport!),
        _ => null,
    };

    public OperatingHours? TryGetHours() =>
        DisplayFormat.TryParseTimeInput(OpeningText, isEndOfDay: false, out var opening)
        && DisplayFormat.TryParseTimeInput(ClosingText, isEndOfDay: true, out var closing)
            ? new OperatingHours(opening, closing)
            : null;

    private string? ValidateHours() =>
        TryGetHours() is not { } hours
            ? "Poné la hora de apertura y de cierre."
            : OperatingHours.Validate(hours.OpeningMinuteOfDay, hours.ClosingMinuteOfDay);

    private static string? ValidatePricing(SportDraft sport)
    {
        if (sport.Price is not { } price || price <= 0) return "Poné cuánto cobrás el turno.";
        if (!sport.HasNightPrice) return null;

        return sport.NightPrice is not { } night || night <= 0 ? "Poné el precio de la noche." : PriceRuleValidator.ValidatePrice(night);
    }

    private static SportSetupDto ToDto(SportDraft sport) => new(
        sport.Sport,
        sport.CourtCount,
        sport.SlotMinutes,
        sport.Price,
        sport.HasNightPrice ? sport.NightPrice : null,
        sport.HasNightPrice ? SetupLimits.NightStartMinuteOfDay : null);

    private static SportDraft FromDto(SportSetupDto saved) => new(saved.Sport)
    {
        CourtCount = saved.CourtCount,
        SlotMinutes = saved.SlotMinutes,
        Price = saved.Price,
        HasNightPrice = saved.NightPrice is not null,
        NightPrice = saved.NightPrice,
    };
}
