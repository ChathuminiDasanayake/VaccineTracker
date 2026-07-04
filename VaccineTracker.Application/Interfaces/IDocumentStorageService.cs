namespace VaccineTracker.Application.Interfaces;

public interface IDocumentStorageService
{
    Task<DocumentStorageResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<DocumentStorageFile> OpenReadAsync(
        string blobName,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}

public sealed record DocumentStorageResult(
    string BlobName);

public sealed record DocumentStorageFile(
    Stream Content,
    string FileName,
    string ContentType);
