using ConCancha.Domain.Clubs;

namespace ConCancha.Domain.Customers;

/// <summary>A person who books at a club, identified inside the club by their normalized WhatsApp.</summary>
public class Customer : IBelongsToClub
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string NormalizedPhone { get; set; } = string.Empty;
    public string? Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
