using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Platform;
using ConCancha.Infrastructure.Identity;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

/// <summary>
/// Sign-in for ConCancha's own panel. Its one account lives in configuration, not in Identity, so no club account
/// can reach it. Every refusal gets the same 401, and the address is blocked after repeated failures.
/// </summary>
public sealed class PlatformSignInService(
    PlatformCredentials credentials,
    PlatformLoginThrottle throttle,
    SessionFactory sessionFactory,
    ILogger<PlatformSignInService> logger) : IPlatformSignInService
{
    public Task<Result<SessionResponse>> SignInAsync(
        PlatformLoginRequest request, string clientAddress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SignIn(request, clientAddress));
    }

    private Result<SessionResponse> SignIn(PlatformLoginRequest request, string clientAddress)
    {
        if (throttle.RetryAfter(clientAddress) is { } wait)
        {
            logger.LogWarning("Platform sign-in blocked for {ClientAddress}", clientAddress);
            return Result<SessionResponse>.Failure(ServiceError.TooManyRequests(PlatformMessages.TooManyAttempts(RoundUpMinutes(wait))));
        }

        if (!credentials.Matches(request.User, request.Password))
        {
            throttle.RecordFailure(clientAddress);
            logger.LogWarning("Platform sign-in refused for {ClientAddress}", clientAddress);
            return Result<SessionResponse>.Failure(ServiceError.Unauthorized(PlatformMessages.InvalidCredentials));
        }

        throttle.Clear(clientAddress);
        logger.LogInformation("Platform admin signed in from {ClientAddress}", clientAddress);
        return Result<SessionResponse>.Success(sessionFactory.CreatePlatformAdmin(request.User, credentials.CurrentFingerprint!));
    }

    private static int RoundUpMinutes(TimeSpan wait) => (int)Math.Ceiling(wait.TotalMinutes);
}
