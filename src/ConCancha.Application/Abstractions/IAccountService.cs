using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;

namespace ConCancha.Application.Abstractions;

public interface IAccountService
{
    Task<Result<SessionResponse>> RegisterAccountAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result<SessionResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task SendPasswordResetLinkAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
