using ConCancha.Application.Messages;
using ConCancha.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.ErrorHandling;

/// <summary>Logs what the person must not see (stack trace, club, path) and answers a generic problem in Spanish.</summary>
public sealed class UnhandledExceptionHandler(
    ILogger<UnhandledExceptionHandler> logger,
    IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {Method} {Path} was cancelled by the client", context.Request.Method, context.Request.Path);
            return true;
        }

        logger.LogError(
            exception, "Unhandled exception on {Method} {Path} (club {ClubId})",
            context.Request.Method, context.Request.Path, context.User.FindFirst(SessionClaims.ClubId)?.Value);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = CommonMessages.UnexpectedError },
        }).ConfigureAwait(false);
    }
}
