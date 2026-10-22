using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConCancha.Api.Modules;

/// <summary>
/// Answers 404 when the session's club hid the section this endpoint belongs to.
/// Needs the club in the request, so it only fits endpoints behind the panel's login.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresModuleAttribute(ClubModules module) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var appearance = context.HttpContext.RequestServices.GetRequiredService<IClubAppearanceService>();
        if (await appearance.IsModuleEnabledAsync(module, context.HttpContext.RequestAborted))
        {
            await next();
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = ClubMessages.SectionDisabled,
        })
        { StatusCode = StatusCodes.Status404NotFound };
    }
}
