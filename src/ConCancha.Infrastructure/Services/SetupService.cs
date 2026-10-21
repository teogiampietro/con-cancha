using System.Text.Json;
using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Setup;
using ConCancha.Domain.Common;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Pricing;
using ConCancha.Domain.Setup;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

public sealed class SetupService(ConCanchaDbContext db, CurrentClubProvider currentClub) : ISetupService
{
    private const string HoursField = "openingMinuteOfDay";
    private const string SportsField = "sports";
    private const string PriceField = "price";
    private const string StepField = "step";
    private const int MaxStep = 40;

    public async Task<SetupProgressDto> GetProgressAsync(CancellationToken cancellationToken)
    {
        var setup = await db.ClubSetups.AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (setup is null) return new SetupProgressDto(0, null, false);

        var draft = setup.DraftJson is null ? null : JsonSerializer.Deserialize<SetupAnswersDto>(setup.DraftJson);
        return new SetupProgressDto(setup.Step, draft, setup.CompletedAt is not null);
    }

    public async Task<Result> SaveProgressAsync(SaveSetupProgressRequest request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request.Draft);
        if (request.Step is < 0 or > MaxStep || json.Length > SetupLimits.DraftMaxLength)
            return Result.Failure(ServiceError.Validation(StepField, "No pudimos guardar tu avance."));

        var setup = await GetOrCreateSetupAsync(cancellationToken).ConfigureAwait(false);
        if (setup.CompletedAt is not null) return Result.Failure(ServiceError.Conflict(SetupMessages.AlreadyCompleted));

        setup.Step = request.Step;
        setup.DraftJson = json;
        setup.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> CompleteAsync(SetupAnswersDto answers, CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        if (club is null) return Result.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        var setup = await GetOrCreateSetupAsync(cancellationToken).ConfigureAwait(false);
        if (setup.CompletedAt is not null) return Result.Failure(ServiceError.Conflict(SetupMessages.AlreadyCompleted));
        if (Validate(answers) is { } invalid) return Result.Failure(invalid);

        club.OpeningMinuteOfDay = answers.OpeningMinuteOfDay;
        club.ClosingMinuteOfDay = answers.ClosingMinuteOfDay;
        await AddCourtsAsync(answers.Sports, cancellationToken).ConfigureAwait(false);

        setup.CompletedAt = DateTimeOffset.UtcNow;
        setup.UpdatedAt = setup.CompletedAt.Value;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<ClubSetup> GetOrCreateSetupAsync(CancellationToken cancellationToken)
    {
        var setup = await db.ClubSetups.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (setup is not null) return setup;

        setup = new ClubSetup();
        db.ClubSetups.Add(setup);
        return setup;
    }

    private async Task AddCourtsAsync(IReadOnlyList<SportSetupDto> sports, CancellationToken cancellationToken)
    {
        var usedNames = (await db.Courts.Select(court => court.Name).ToListAsync(cancellationToken).ConfigureAwait(false)).ToHashSet();
        foreach (var sport in sports)
        {
            foreach (var court in BuildCourts(sport, usedNames))
            {
                db.Courts.Add(court);
                db.PriceRules.AddRange(BuildPriceRules(court.Id, sport));
            }
        }
    }

    private static IEnumerable<Court> BuildCourts(SportSetupDto sport, HashSet<string> usedNames)
    {
        var number = 1;
        for (var created = 0; created < sport.CourtCount; created++)
        {
            var name = $"{SportLabels.Get(sport.Sport)} {number++}";
            while (!usedNames.Add(name)) name = $"{SportLabels.Get(sport.Sport)} {number++}";
            yield return new Court { Name = name, Sport = sport.Sport, SlotMinutes = sport.SlotMinutes };
        }
    }

    private static IEnumerable<PriceRule> BuildPriceRules(Guid courtId, SportSetupDto sport)
    {
        yield return NewRule(courtId, 0, sport.Price!.Value);
        if (sport.NightPrice is { } nightPrice)
            yield return NewRule(courtId, sport.NightFromMinuteOfDay!.Value, nightPrice);
    }

    private static PriceRule NewRule(Guid courtId, int fromMinuteOfDay, decimal price) => new()
    {
        CourtId = courtId,
        FromMinuteOfDay = fromMinuteOfDay,
        ToMinuteOfDay = TimeConstants.MinutesPerDay,
        Price = price,
    };

    private static ServiceError? Validate(SetupAnswersDto answers)
    {
        if (OperatingHours.Validate(answers.OpeningMinuteOfDay, answers.ClosingMinuteOfDay) is { } reason)
            return ServiceError.Validation(HoursField, reason);
        if (answers.Sports.Count == 0) return ServiceError.Validation(SportsField, SetupMessages.SportsRequired);
        if (answers.Sports.Select(sport => sport.Sport).Distinct().Count() != answers.Sports.Count)
            return ServiceError.Validation(SportsField, SetupMessages.DuplicateSport);

        return answers.Sports.Select(sport => ValidateSport(sport, answers)).FirstOrDefault(error => error is not null);
    }

    private static ServiceError? ValidateSport(SportSetupDto sport, SetupAnswersDto answers)
    {
        if (!Enum.IsDefined(sport.Sport)) return ServiceError.Validation(SportsField, "Elegí un deporte.");
        if (sport.CourtCount is < 1 or > CourtLimits.MaxCourtsPerSport)
            return ServiceError.Validation(SportsField, SetupMessages.InvalidCourtCount);
        if (!CourtLimits.AllowedSlotMinutes.Contains(sport.SlotMinutes))
            return ServiceError.Validation(SportsField, CourtMessages.InvalidSlotMinutes);

        return ValidatePrices(sport, answers);
    }

    private static ServiceError? ValidatePrices(SportSetupDto sport, SetupAnswersDto answers)
    {
        if (sport.Price is not { } price || PriceRuleValidator.ValidatePrice(price) is not null)
            return ServiceError.Validation(PriceField, SetupMessages.PriceRequired);
        if ((sport.NightPrice is null) != (sport.NightFromMinuteOfDay is null))
            return ServiceError.Validation(PriceField, SetupMessages.NightPriceIncomplete);
        if (sport.NightPrice is not { } night) return null;
        if (PriceRuleValidator.ValidatePrice(night) is { } reason) return ServiceError.Validation(PriceField, reason);

        var from = sport.NightFromMinuteOfDay!.Value;
        return from <= answers.OpeningMinuteOfDay || from >= answers.ClosingMinuteOfDay
            ? ServiceError.Validation(PriceField, SetupMessages.InvalidNightStart)
            : null;
    }
}
