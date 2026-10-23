namespace ConCancha.Web.Services.Landing;

/// <summary><see cref="IsAvailable"/> false shows the plan with its button disabled: it cannot be bought yet.</summary>
public sealed record LandingPlan(
    string Name,
    string? Badge,
    string Price,
    string? Period,
    string Tagline,
    IReadOnlyList<string> Includes,
    string ButtonText,
    bool IsAvailable,
    bool IsFeatured = false);
