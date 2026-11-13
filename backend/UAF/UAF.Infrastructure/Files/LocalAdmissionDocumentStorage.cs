using Microsoft.Extensions.Options;
using UAF.Application.Features.Admissions.Abstractions;

namespace UAF.Infrastructure.Files;

/// <summary>
/// Development/local provider. Files are kept outside wwwroot and are never served statically.
/// Use a private object-storage provider for a multi-instance production deployment.
/// </summary>
internal sealed class LocalAdmissionDocumentStorage : IAdmissionDocumentStorage
{
    private readonly string _rootPath;

    public LocalAdmissionDocumentStorage(IOptions<AdmissionDocumentStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var configured = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Admission document storage root path is required.");
        _rootPath = Path.GetFullPath(configured);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var key = $"{Guid.NewGuid():N}.bin";
        var path = ResolvePath(key);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            81920, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await content.CopyToAsync(output, ct);
        await output.FlushAsync(ct);
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (!File.Exists(path)) throw new FileNotFoundException("Admission document file was not found.");
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task DeleteIfExistsAsync(string storageKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length != 36 || !key.EndsWith(".bin", StringComparison.Ordinal) ||
            !Guid.TryParseExact(key[..32], "N", out _))
            throw new ArgumentException("Invalid admission document storage key.", nameof(key));

        var path = Path.GetFullPath(Path.Combine(_rootPath, key));
        var prefix = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved file path is outside the private storage directory.");
        return path;
    }
}
