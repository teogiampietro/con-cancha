using ConCancha.Application.Results;
using ConCancha.Contracts.Setup;

namespace ConCancha.Application.Abstractions;

public interface ISetupService
{
    Task<SetupProgressDto> GetProgressAsync(CancellationToken cancellationToken);

    Task<Result> SaveProgressAsync(SaveSetupProgressRequest request, CancellationToken cancellationToken);

    Task<Result> CompleteAsync(SetupAnswersDto answers, CancellationToken cancellationToken);
}
