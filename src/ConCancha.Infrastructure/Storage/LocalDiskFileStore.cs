using ConCancha.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace ConCancha.Infrastructure.Storage;

public sealed class LocalDiskFileStore(IOptions<FileStorageOptions> options) : IFileStore
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken cancellationToken)
    {
        var key = $"{folder}/{Guid.NewGuid():N}.{extension}";
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        return key;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        Stream? stream = File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(ResolvePath(key));
        return Task.CompletedTask;
    }

    private string ResolvePath(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("The key points outside the storage folder.", nameof(key));
        return path;
    }
}
