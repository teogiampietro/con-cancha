namespace ConCancha.Domain.Clubs;

public class ClubInvitation : IBelongsToClub
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Only the hash is stored, so a database leak does not expose invitation links.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public ClubRole Role { get; set; } = ClubRole.Staff;
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsPendingAt(DateTimeOffset now) => Status == InvitationStatus.Pending && ExpiresAt > now;
}
