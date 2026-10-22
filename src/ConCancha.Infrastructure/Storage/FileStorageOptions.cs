namespace ConCancha.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Folder that holds every uploaded file. In production it must be a persistent volume.</summary>
    public string RootPath { get; set; } = "storage";
}
