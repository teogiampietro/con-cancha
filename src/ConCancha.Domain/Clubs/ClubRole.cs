namespace ConCancha.Domain.Clubs;

public enum ClubRole
{
    /// <summary>Whole panel, including settings and team.</summary>
    Owner = 1,

    /// <summary>Front desk: books and charges; cannot see or change settings or team.</summary>
    Staff = 2,
}
