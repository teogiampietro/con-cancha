using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;

namespace ConCancha.Application.Abstractions;

public interface IInvitationAcceptor
{
    Task<Result<SessionResponse>> AcceptAsync(AcceptInvitationRequest request, CancellationToken cancellationToken);
}
