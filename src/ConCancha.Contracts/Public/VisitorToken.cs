namespace ConCancha.Contracts.Public;

/// <summary>Secret a browser keeps to recognise its own bookings without an account. The server stores only its hash.</summary>
public static class VisitorToken
{
    public const string HeaderName = "X-Visitor-Token";
    public const int MinLength = 32;
    public const int MaxLength = 128;

    public static bool IsValid(string? token) =>
        token is { Length: >= MinLength and <= MaxLength } && token.All(char.IsAsciiLetterOrDigit);
}
