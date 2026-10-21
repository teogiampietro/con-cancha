namespace ConCancha.Contracts.Public;

/// <param name="ManagementToken">Secret of the management link. It is shown once and only its hash is stored.</param>
public record PublicBookingCreatedDto(string ManagementToken, PublicBookingDto Booking);
