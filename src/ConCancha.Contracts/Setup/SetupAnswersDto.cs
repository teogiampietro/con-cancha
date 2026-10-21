namespace ConCancha.Contracts.Setup;

public record SetupAnswersDto(
    IReadOnlyList<SportSetupDto> Sports,
    int OpeningMinuteOfDay,
    int ClosingMinuteOfDay);
