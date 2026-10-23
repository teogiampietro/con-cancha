using ConCancha.Application.Abstractions;
using ConCancha.Application.Emails;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;
using ConCancha.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

public sealed class EmailVerifier(
    UserManager<AppUser> users,
    IEmailSender emailSender,
    ILogger<EmailVerifier> logger)
{
    private const string VerificationPurpose = "EmailVerification";

    public async Task SendCodeAsync(AppUser user, CancellationToken cancellationToken)
    {
        var code = await users.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, VerificationPurpose)
            .ConfigureAwait(false);
        var email = EmailTemplates.EmailVerification(code);

        await emailSender.SendAsync(user.Email!, email.Subject, email.Body, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> ConfirmAsync(AppUser user, string code)
    {
        if (user.EmailConfirmed)
            return Result.Success();

        if (await users.IsLockedOutAsync(user).ConfigureAwait(false))
            return Result.Failure(ServiceError.Locked(AccountMessages.TooManyAttempts));

        var isValid = await users.VerifyUserTokenAsync(
            user, TokenOptions.DefaultEmailProvider, VerificationPurpose, code.Trim()).ConfigureAwait(false);
        if (!isValid)
            return await RejectCodeAsync(user).ConfigureAwait(false);

        user.EmailConfirmed = true;
        await users.UpdateAsync(user).ConfigureAwait(false);
        await users.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<Result> RejectCodeAsync(AppUser user)
    {
        await users.AccessFailedAsync(user).ConfigureAwait(false);
        logger.LogWarning("Wrong verification code for user {UserId}", user.Id);
        return Result.Failure(ServiceError.Validation(
            nameof(VerifyEmailRequest.Code), AccountMessages.InvalidVerificationCode));
    }
}
