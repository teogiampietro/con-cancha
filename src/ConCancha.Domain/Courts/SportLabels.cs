namespace ConCancha.Domain.Courts;

/// <summary>Spanish names shown to people and used to name courts ("Pádel 1").</summary>
public static class SportLabels
{
    public static string Get(Sport sport) => sport switch
    {
        Sport.Padel => "Pádel",
        Sport.Football => "Fútbol",
        Sport.Tennis => "Tenis",
        Sport.Pickleball => "Pickleball",
        Sport.FiveASideFootball => "Fútbol 5",
        Sport.Volleyball => "Vóley",
        Sport.Basketball => "Básquet",
        _ => sport.ToString(),
    };
}
