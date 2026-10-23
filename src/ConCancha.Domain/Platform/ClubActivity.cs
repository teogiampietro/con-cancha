namespace ConCancha.Domain.Platform;

public enum ClubActivity
{
    /// <summary>Signed up recently and has not booked yet: still setting up.</summary>
    New = 1,

    /// <summary>Has turns this week or the previous one.</summary>
    Active = 2,

    /// <summary>No turns in the last two weeks.</summary>
    Inactive = 3,
}
