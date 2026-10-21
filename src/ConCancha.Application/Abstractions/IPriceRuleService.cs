using ConCancha.Application.Results;
using ConCancha.Contracts.Pricing;

namespace ConCancha.Application.Abstractions;

public interface IPriceRuleService
{
    Task<Result<IReadOnlyList<PriceRuleDto>>> ListByCourtAsync(Guid courtId, CancellationToken cancellationToken);

    Task<Result<PriceRuleDto>> CreateAsync(Guid courtId, PriceRuleRequest request, CancellationToken cancellationToken);

    Task<Result<PriceRuleDto>> UpdateAsync(Guid ruleId, PriceRuleRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid ruleId, CancellationToken cancellationToken);
}
