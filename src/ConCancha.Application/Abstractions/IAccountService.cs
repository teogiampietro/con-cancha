using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;

namespace ConCancha.Application.Abstractions;

public interface IAccountService
{
    Task<Result> RegisterAccountAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result<SessionResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);

    Task ResendVerificationCodeAsync(ResendVerificationRequest request, CancellationToken cancellationToken);

    Task<Result<SessionResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task SendPasswordResetLinkAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken);
}
