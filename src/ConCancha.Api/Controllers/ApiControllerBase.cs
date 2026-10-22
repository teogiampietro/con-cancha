using ConCancha.Application.Results;
using ConCancha.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected string CurrentUserId => User.FindFirst(SessionClaims.UserId)!.Value;

    protected IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value!) : ToProblem(result.Error!);

    protected IActionResult ToActionResult(Result result, Func<IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : ToProblem(result.Error!);

    protected IActionResult ToProblem(ServiceError error) =>
        error.Kind == ErrorKind.Validation
            ? ValidationProblem(ToValidationDetails(error))
            : Problem(statusCode: ToStatusCode(error.Kind), title: error.Message);

    private static ValidationProblemDetails ToValidationDetails(ServiceError error) =>
        new(error.FieldErrors?.ToDictionary(field => field.Key, field => field.Value) ?? []);

    private static int ToStatusCode(ErrorKind kind) => kind switch
    {
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Locked => StatusCodes.Status423Locked,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status500InternalServerError,
    };
}
