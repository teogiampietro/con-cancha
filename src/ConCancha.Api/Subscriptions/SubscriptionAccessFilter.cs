using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConCancha.Api.Subscriptions;

/// <summary>
/// Answers 402 to a signed-in club whose trial is over without a subscription. Requests without a club token, such as
/// the public site's, are not checked here.
/// </summary>
public sealed class SubscriptionAccessFilter(IClubSubscriptionService subscriptions) : IAsyncActionFilter
{
    private const int PaymentRequired = StatusCodes.Status402PaymentRequired;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!RequiresSubscription(context) || await subscriptions.HasAccessAsync(context.HttpContext.RequestAborted))
        {
            await next();
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails { Status = PaymentRequired, Title = SubscriptionMessages.TrialExpired })
        {
            StatusCode = PaymentRequired,
        };
    }

    private static bool RequiresSubscription(ActionExecutingContext context) =>
        context.HttpContext.User.HasClaim(claim => claim.Type == SessionClaims.ClubId)
        && !context.ActionDescriptor.EndpointMetadata.Any(metadata => metadata is AllowWithoutSubscriptionAttribute);
}
