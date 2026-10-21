using ConCancha.Domain.Clubs;

namespace ConCancha.Domain.Cash;

/// <summary>A day's cash marked as closed. It only reports: the day can still change and the closure can be reopened.</summary>
public class CashClosure : IBelongsToClub
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }

    /// <summary>Day in the club's time zone.</summary>
    public DateOnly Date { get; set; }

    public decimal Total { get; set; }
    public DateTimeOffset ClosedAt { get; set; } = DateTimeOffset.UtcNow;
}
