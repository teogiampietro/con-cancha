using ConCancha.Domain.Clubs;

namespace ConCancha.Domain.Courts;

public class Court : IBelongsToClub
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }
    public Club? Club { get; set; }

    public string Name { get; set; } = string.Empty;
    public Sport Sport { get; set; }
    public int SlotMinutes { get; set; }

    /// <summary>Exception to the club's hours. Both are set or both are null.</summary>
    public int? OpeningMinuteOfDay { get; set; }
    public int? ClosingMinuteOfDay { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
