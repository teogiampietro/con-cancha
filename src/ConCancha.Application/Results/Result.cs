namespace ConCancha.Application.Results;

public sealed record Result(ServiceError? Error)
{
    public bool IsSuccess => Error is null;

    public static Result Success() => new((ServiceError?)null);

    public static Result Failure(ServiceError error) => new(error);
}
