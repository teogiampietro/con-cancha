namespace ConCancha.Infrastructure.Services;

public sealed class WebAppOptions
{
    public const string SectionName = "Web";

    public string BaseUrl { get; set; } = "http://localhost:5141";
}
