namespace ConCancha.Contracts.Clubs;

public record SlugAvailabilityResponse(string Slug, bool IsAvailable, string? Reason);
