namespace ConCancha.Contracts.Reminders;

public record ReminderListDto(string ClubName, IReadOnlyList<ReminderDto> Reminders);
