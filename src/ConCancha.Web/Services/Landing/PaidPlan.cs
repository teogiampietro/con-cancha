namespace ConCancha.Web.Services.Landing;

/// <summary>Leave <see cref="Price"/> and <see cref="Period"/> null until the owner defines them; the page shows a visible placeholder.</summary>
public sealed record PaidPlan(string? Price, string? Period, IReadOnlyList<string> Includes);
