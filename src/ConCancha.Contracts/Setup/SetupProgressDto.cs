namespace ConCancha.Contracts.Setup;

public record SetupProgressDto(int Step, SetupAnswersDto? Draft, bool IsCompleted);
