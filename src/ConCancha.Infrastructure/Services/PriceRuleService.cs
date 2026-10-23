using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Pricing;
using ConCancha.Domain.Pricing;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Services.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

public sealed class PriceRuleService(ConCanchaDbContext db, RecurringBookingScheduler recurringScheduler) : IPriceRuleService
{
    private const string PriceField = "price";

    public async Task<Result<IReadOnlyList<PriceRuleDto>>> ListByCourtAsync(Guid courtId, CancellationToken cancellationToken)
    {
        if (!await CourtExistsAsync(courtId, cancellationToken).ConfigureAwait(false))
            return Result<IReadOnlyList<PriceRuleDto>>.Failure(ServiceError.NotFound(CourtMessages.CourtNotFound));

        var rules = await db.PriceRules.AsNoTracking()
            .Where(rule => rule.CourtId == courtId)
            .OrderBy(rule => rule.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<PriceRuleDto>>.Success(rules.Select(CourtMapper.ToDto).ToList());
    }

    public async Task<Result<PriceRuleDto>> CreateAsync(Guid courtId, PriceRuleRequest request, CancellationToken cancellationToken)
    {
        if (Validate(request) is { } invalid) return Result<PriceRuleDto>.Failure(invalid);
        if (!await CourtExistsAsync(courtId, cancellationToken).ConfigureAwait(false))
            return Result<PriceRuleDto>.Failure(ServiceError.NotFound(CourtMessages.CourtNotFound));

        var rule = Apply(new PriceRule { CourtId = courtId }, request);
        db.PriceRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await recurringScheduler.RepriceUpcomingAsync(courtId, cancellationToken).ConfigureAwait(false);
        return Result<PriceRuleDto>.Success(CourtMapper.ToDto(rule));
    }

    public async Task<Result<PriceRuleDto>> UpdateAsync(Guid ruleId, PriceRuleRequest request, CancellationToken cancellationToken)
    {
        if (Validate(request) is { } invalid) return Result<PriceRuleDto>.Failure(invalid);

        var rule = await db.PriceRules.FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken).ConfigureAwait(false);
        if (rule is null) return Result<PriceRuleDto>.Failure(ServiceError.NotFound(CourtMessages.PriceRuleNotFound));

        Apply(rule, request);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await recurringScheduler.RepriceUpcomingAsync(rule.CourtId, cancellationToken).ConfigureAwait(false);
        return Result<PriceRuleDto>.Success(CourtMapper.ToDto(rule));
    }

    public async Task<Result> DeleteAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        var rule = await db.PriceRules.FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken).ConfigureAwait(false);
        if (rule is null) return Result.Failure(ServiceError.NotFound(CourtMessages.PriceRuleNotFound));

        db.PriceRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await recurringScheduler.RepriceUpcomingAsync(rule.CourtId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private Task<bool> CourtExistsAsync(Guid courtId, CancellationToken cancellationToken) =>
        db.Courts.AnyAsync(court => court.Id == courtId, cancellationToken);

    private static PriceRule Apply(PriceRule rule, PriceRuleRequest request)
    {
        rule.Days = request.Days;
        rule.FromMinuteOfDay = request.FromMinuteOfDay;
        rule.ToMinuteOfDay = request.ToMinuteOfDay;
        rule.Price = request.Price;
        return rule;
    }

    private static ServiceError? Validate(PriceRuleRequest request) =>
        PriceRuleValidator.Validate(request.Days, request.FromMinuteOfDay, request.ToMinuteOfDay, request.Price) is { } reason
            ? ServiceError.Validation(PriceField, reason)
            : null;
}
