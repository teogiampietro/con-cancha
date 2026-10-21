using System.Text;
using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Identity;
using ConCancha.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ConCancha.Infrastructure.Services;

public sealed class AccountService(
    UserManager<AppUser> users,
    ConCanchaDbContext db,
    SessionFactory sessionFactory,
    IEmailSender emailSender,
    IOptions<WebAppOptions> webOptions) : IAccountService
{
    private const string ResetPasswordPath = "/panel/restablecer";

    public async Task<Result<SessionResponse>> RegisterAccountAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = new AppUser { UserName = email, Email = email };

        var creation = await users.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (creation.Succeeded)
            return Result<SessionResponse>.Success(sessionFactory.Create(user, club: null, role: null));

        return Result<SessionResponse>.Failure(ToRegistrationError(creation));
    }

    public async Task<Result<SessionResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (user is null)
            return Result<SessionResponse>.Failure(ServiceError.Unauthorized(AccountMessages.InvalidCredentials));

        if (await RejectCredentialsAsync(user, request.Password).ConfigureAwait(false) is { } error)
            return Result<SessionResponse>.Failure(error);

        await users.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        var membership = await FindFirstMembershipAsync(user.Id, cancellationToken).ConfigureAwait(false);

        return Result<SessionResponse>.Success(sessionFactory.Create(user, membership?.Club, membership?.Role));
    }

    public async Task SendPasswordResetLinkAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (user is null) return;

        var token = await users.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        var body = string.Format(AccountMessages.PasswordResetBodyTemplate, BuildResetLink(user.Email!, token));

        await emailSender.SendAsync(user.Email!, AccountMessages.PasswordResetSubject, body, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        var token = DecodeToken(request.Token);
        if (user is null || token is null)
            return Result.Failure(InvalidTokenError());

        var reset = await users.ResetPasswordAsync(user, token, request.NewPassword).ConfigureAwait(false);
        if (!reset.Succeeded)
            return Result.Failure(IdentityErrorMapper.ToValidationError(reset, nameof(ResetPasswordRequest.NewPassword)));

        await UnlockAsync(user).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<ServiceError?> RejectCredentialsAsync(AppUser user, string password)
    {
        if (await users.IsLockedOutAsync(user).ConfigureAwait(false))
            return ServiceError.Locked(AccountMessages.TooManyAttempts);

        if (await users.CheckPasswordAsync(user, password).ConfigureAwait(false))
            return null;

        await users.AccessFailedAsync(user).ConfigureAwait(false);
        return ServiceError.Unauthorized(AccountMessages.InvalidCredentials);
    }

    private Task<ClubMember?> FindFirstMembershipAsync(string userId, CancellationToken cancellationToken) =>
        db.ClubMembers.IgnoreQueryFilters()
            .Include(member => member.Club)
            .Where(member => member.UserId == userId)
            .OrderBy(member => member.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task UnlockAsync(AppUser user)
    {
        await users.SetLockoutEndDateAsync(user, null).ConfigureAwait(false);
        await users.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
    }

    private string BuildResetLink(string email, string token)
    {
        var baseUrl = webOptions.Value.BaseUrl.TrimEnd('/');
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        return $"{baseUrl}{ResetPasswordPath}?mail={Uri.EscapeDataString(email)}&token={encodedToken}";
    }

    private static string? DecodeToken(string encodedToken)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static ServiceError ToRegistrationError(IdentityResult creation) =>
        IdentityErrorMapper.IsDuplicateEmail(creation)
            ? ServiceError.Conflict(AccountMessages.EmailAlreadyRegistered)
            : IdentityErrorMapper.ToValidationError(creation, nameof(RegisterRequest.Password));

    private static ServiceError InvalidTokenError() =>
        IdentityErrorMapper.ToValidationError(
            IdentityResult.Failed(new SpanishIdentityErrorDescriber().InvalidToken()),
            nameof(ResetPasswordRequest.Token));
}
