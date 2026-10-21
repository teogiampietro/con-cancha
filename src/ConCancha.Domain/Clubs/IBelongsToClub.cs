namespace ConCancha.Domain.Clubs;

/// <summary>
/// Marks entities owned by a club. The data layer applies a global filter on <see cref="ClubId"/>
/// so a query never returns another club's data.
/// </summary>
public interface IBelongsToClub
{
    Guid ClubId { get; set; }
}
