using Microsoft.Extensions.Configuration;
using VaccineTracker.Application.Interfaces;

namespace VaccineTracker.Infrastructure.Services;

public sealed class LocalDocumentStorageService : IDocumentStorageService
{
    private readonly string _rootPath;

    public LocalDocumentStorageService(IConfiguration configuration)
    {
        _rootPath = configuration["DocumentStorage:LocalPath"] ??
            Path.Combine(AppContext.BaseDirectory, "documents");
    }

    public async Task<DocumentStorageResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);

        var extension = Path.GetExtension(fileName);
        var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";
        var fullPath = GetSafeFullPath(blobName);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return new DocumentStorageResult(blobName);
    }

    public Task<DocumentStorageFile> OpenReadAsync(
        string blobName,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafeFullPath(blobName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Document file was not found.", blobName);
        }

        Stream stream = File.OpenRead(fullPath);

        return Task.FromResult(
            new DocumentStorageFile(
                stream,
                fileName,
                contentType));
    }

    private string GetSafeFullPath(string blobName)
    {
        var fullPath = Path.GetFullPath(
            Path.Combine(_rootPath, blobName.Replace('/', Path.DirectorySeparatorChar)));

        var rootFullPath = Path.GetFullPath(_rootPath);

        if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid document path.");
        }

        return fullPath;
    }
}
