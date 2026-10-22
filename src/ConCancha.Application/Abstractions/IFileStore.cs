namespace ConCancha.Application.Abstractions;

/// <summary>Where uploaded files live. Callers keep only the returned key and never see the storage location.</summary>
public interface IFileStore
{
    Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
