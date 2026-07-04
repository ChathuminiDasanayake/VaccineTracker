using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Documents;

namespace VaccineTracker.Application.Interfaces;

public interface IDocumentsService
{
    Task<PagedResponse<DocumentResponse>> GetDocumentsAsync(
        GetDocumentsRequest request,
        CancellationToken cancellationToken = default);

    Task<DocumentResponse> GetDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<DocumentResponse> UploadDocumentAsync(
        UploadDocumentRequest request,
        Stream content,
        string fileName,
        string contentType,
        long sizeInBytes,
        CancellationToken cancellationToken = default);

    Task<DocumentStorageFile> DownloadDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
