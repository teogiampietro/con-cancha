namespace ConCancha.Contracts.Reminders;

/// <param name="WhatsAppPhone">Digits for a wa.me link (country code included).</param>
public record ReminderDto(
    Guid BookingId,
    string CustomerName,
    string WhatsAppPhone,
    string CourtName,
    DateOnly Date,
    int StartMinuteOfDay,
    bool IsSent);
