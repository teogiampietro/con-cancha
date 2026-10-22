namespace ConCancha.Domain.Clubs;

/// <summary>Optional menu sections a club can show or hide. A new module adds a flag here.</summary>
[Flags]
public enum ClubModules
{
    None = 0,
    Buffet = 1,
    Tournaments = 2,
    All = Buffet | Tournaments,
}
