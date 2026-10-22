using ConCancha.Api.ErrorHandling;
using ConCancha.Application.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConCancha.Tests.Api;

public class UnhandledExceptionHandlerTests
{
    [Fact]
    public async Task UnexpectedException_AnswersAGenericProblemWithoutDetails()
    {
        var problems = new CapturingProblemDetailsService();
        var handler = new UnhandledExceptionHandler(NullLogger<UnhandledExceptionHandler>.Instance, problems);
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("secret detail"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(CommonMessages.UnexpectedError, problems.Written!.ProblemDetails.Title);
        Assert.DoesNotContain("secret", problems.Written.ProblemDetails.Detail ?? string.Empty);
    }

    [Fact]
    public async Task RequestCancelledByTheClient_IsNotAnError()
    {
        var problems = new CapturingProblemDetailsService();
        var handler = new UnhandledExceptionHandler(NullLogger<UnhandledExceptionHandler>.Instance, problems);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var context = new DefaultHttpContext { RequestAborted = cancelled.Token };

        var handled = await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Null(problems.Written);
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.FromResult(true);
        }
    }
}
