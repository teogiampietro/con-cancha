using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Platform;

namespace ConCancha.Application.Abstractions;

public interface IPlatformSignInService
{
    Task<Result<SessionResponse>> SignInAsync(PlatformLoginRequest request, string clientAddress, CancellationToken cancellationToken);
}
