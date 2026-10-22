namespace ConCancha.Contracts.Public;

/// <param name="CancelableUntil">Last moment the visitor can still cancel online.</param>
public record PublicMyBookingDto(
    Guid Id,
    string CourtName,
    DateOnly Date,
    int StartMinuteOfDay,
    int EndMinuteOfDay,
    decimal Price,
    bool CanCancel,
    DateTimeOffset CancelableUntil);
