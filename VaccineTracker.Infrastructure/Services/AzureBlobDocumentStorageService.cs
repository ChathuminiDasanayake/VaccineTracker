using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using VaccineTracker.Application.Interfaces;

namespace VaccineTracker.Infrastructure.Services;

public sealed class AzureBlobDocumentStorageService : IDocumentStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobDocumentStorageService(
        IOptions<BlobStorageSettings> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException(
                "Blob storage connection string is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            throw new InvalidOperationException(
                "Blob storage container name is not configured.");
        }

        _containerClient = new BlobContainerClient(
            settings.ConnectionString,
            settings.ContainerName);
    }

    public async Task<DocumentStorageResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(
            publicAccessType: PublicAccessType.None,
            cancellationToken: cancellationToken);

        var extension = Path.GetExtension(fileName);
        var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            },
            cancellationToken);

        return new DocumentStorageResult(blobName);
    }

    public async Task<DocumentStorageFile> OpenReadAsync(
        string blobName,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            throw new FileNotFoundException(
                "Document file was not found.",
                blobName);
        }

        var stream = await blobClient.OpenReadAsync(
            cancellationToken: cancellationToken);

        return new DocumentStorageFile(
            stream,
            fileName,
            contentType);
    }
}
