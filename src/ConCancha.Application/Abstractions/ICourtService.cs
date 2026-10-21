using ConCancha.Application.Results;
using ConCancha.Contracts.Courts;

namespace ConCancha.Application.Abstractions;

public interface ICourtService
{
    Task<IReadOnlyList<CourtDto>> ListAsync(CancellationToken cancellationToken);

    Task<Result<CourtDto>> CreateAsync(CourtRequest request, CancellationToken cancellationToken);

    Task<Result<CourtDto>> UpdateAsync(Guid courtId, CourtRequest request, CancellationToken cancellationToken);
}
