namespace ConCancha.Contracts.Clubs;

public record ClubMemberDto(string UserId, string Email, string Role, DateTimeOffset CreatedAt);
