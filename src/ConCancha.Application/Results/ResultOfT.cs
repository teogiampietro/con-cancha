namespace ConCancha.Application.Results;

public sealed record Result<T>(T? Value, ServiceError? Error)
{
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(ServiceError error) => new(default, error);
}
