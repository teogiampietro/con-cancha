namespace ConCancha.Contracts.Clubs;

/// <summary><see cref="Id"/> is the membership's for an active person and the invitation's for an invited one.</summary>
public record ClubMemberDto(
    string UserId,
    string Email,
    string Role,
    DateTimeOffset CreatedAt,
    Guid Id,
    string Status,
    DateTimeOffset? ExpiresAt);
