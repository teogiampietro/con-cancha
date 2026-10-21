namespace ConCancha.Web.Services;

public sealed record ApiResult<T>(
    T? Value,
    string? Error,
    IReadOnlyDictionary<string, string[]>? FieldErrors,
    int StatusCode)
{
    private const string FallbackMessage = "Algo salió mal. Probá de nuevo.";

    public bool IsSuccess => Error is null && FieldErrors is null;

    public string GeneralMessage =>
        Error ?? FieldErrors?.Values.SelectMany(messages => messages).FirstOrDefault() ?? FallbackMessage;

    public string? GetFirstFieldError(string field) =>
        FieldErrors is not null && FieldErrors.TryGetValue(field, out var messages) && messages.Length > 0
            ? messages[0]
            : null;

    public static ApiResult<T> Success(T? value, int statusCode) => new(value, null, null, statusCode);

    public static ApiResult<T> Failure(string error, int statusCode) => new(default, error, null, statusCode);

    public static ApiResult<T> Invalid(IReadOnlyDictionary<string, string[]> fieldErrors, int statusCode) =>
        new(default, null, fieldErrors, statusCode);
}
