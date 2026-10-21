using ConCancha.Contracts.Clubs;

namespace ConCancha.Contracts.Accounts;

public record SessionResponse(string Token, DateTimeOffset ExpiresAt, UserDto User, ClubDto? Club, string? Role);
