namespace ConCancha.Domain.Clubs;

public class ClubMember : IBelongsToClub
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }
    public Club? Club { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ClubRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
