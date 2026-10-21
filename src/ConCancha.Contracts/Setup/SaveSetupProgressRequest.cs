namespace ConCancha.Contracts.Setup;

public record SaveSetupProgressRequest(int Step, SetupAnswersDto Draft);
