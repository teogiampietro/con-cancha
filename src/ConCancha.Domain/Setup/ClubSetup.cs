using ConCancha.Domain.Clubs;

namespace ConCancha.Domain.Setup;

/// <summary>Progress of the initial setup wizard, so an owner can leave and resume.</summary>
public class ClubSetup : IBelongsToClub
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }

    public int Step { get; set; }
    public string? DraftJson { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
