namespace ConCancha.Application.Results;

public sealed record ServiceError(
    ErrorKind Kind,
    string Message,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null)
{
    public static ServiceError Validation(string field, params string[] messages) =>
        new(ErrorKind.Validation, messages[0], new Dictionary<string, string[]> { [field] = messages });

    public static ServiceError Conflict(string message) => new(ErrorKind.Conflict, message);

    public static ServiceError Unauthorized(string message) => new(ErrorKind.Unauthorized, message);

    public static ServiceError Locked(string message) => new(ErrorKind.Locked, message);

    public static ServiceError NotFound(string message) => new(ErrorKind.NotFound, message);
}
