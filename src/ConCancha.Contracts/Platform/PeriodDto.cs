namespace ConCancha.Contracts.Platform;

/// <summary>Local days, both ends included.</summary>
public record PeriodDto(DateOnly From, DateOnly To);
