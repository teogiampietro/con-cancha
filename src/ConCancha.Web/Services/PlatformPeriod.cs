namespace ConCancha.Web.Services;

/// <summary>Local days chosen in the platform panel, both ends included.</summary>
public sealed record PlatformPeriod(DateOnly From, DateOnly To);
