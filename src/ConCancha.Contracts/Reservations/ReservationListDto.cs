namespace ConCancha.Contracts.Reservations;

/// <param name="IsTruncated">True when more reservations matched than the list returns.</param>
public record ReservationListDto(IReadOnlyList<ReservationItemDto> Items, bool IsTruncated);
