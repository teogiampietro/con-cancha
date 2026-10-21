namespace ConCancha.Infrastructure.Identity;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool CheckLeakedPasswords { get; set; } = true;
}
