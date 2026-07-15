using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Documents;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class DocumentsService : IDocumentsService
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png"
    ];

    private const long MaxFileSizeInBytes = 10 * 1024 * 1024;

    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly ILogger<DocumentsService> _logger;

    public DocumentsService(
        VaccineTrackerDbContext dbContext,
        ICurrentUser currentUser,
        IDocumentStorageService documentStorageService,
        ILogger<DocumentsService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _documentStorageService = documentStorageService;
        _logger = logger;
    }

    public async Task<PagedResponse<DocumentResponse>> GetDocumentsAsync(
        GetDocumentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = ResolveHospitalId();
        EnsureHospitalAccess(hospitalId);

        var query = _dbContext.Documents
            .AsNoTracking()
            .Where(document =>
                document.HospitalId == hospitalId &&
                !document.IsDeleted);

        if (request.PatientId.HasValue)
        {
            query = query.Where(document =>
                document.PatientId == request.PatientId.Value);
        }

        if (request.VaccinationRecordId.HasValue)
        {
            query = query.Where(document =>
                document.VaccinationRecordId == request.VaccinationRecordId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (!TryParseDocumentType(request.Type, out var type))
            {
                throw new ValidationException(
                    $"Document type '{request.Type}' is invalid.");
            }

            query = query.Where(document => document.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParseEntityStatus(request.Status, out var status))
            {
                throw new ValidationException(
                    $"Status '{request.Status}' is invalid.");
            }

            query = query.Where(document => document.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var documents = await query
            .OrderByDescending(document => document.CreatedAt)
            .ThenBy(document => document.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(document => ToResponse(document))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (totalCount + request.PageSize - 1) / request.PageSize;

        return new PagedResponse<DocumentResponse>(
            documents,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<PagedResponse<DocumentResponse>> GetMyDocumentsAsync(
        GetDocumentsRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePatientRole();

        var patientIds = await GetAccessiblePatientIdsAsync(cancellationToken);

        if (patientIds.Count == 0)
        {
            return new PagedResponse<DocumentResponse>(
                [],
                request.PageNumber,
                request.PageSize,
                0,
                0);
        }

        var query = _dbContext.Documents
            .AsNoTracking()
            .Where(document =>
                !document.IsDeleted &&
                document.Status == EntityStatus.Active &&
                ((document.PatientId.HasValue &&
                    patientIds.Contains(document.PatientId.Value)) ||
                (document.VaccinationRecord != null &&
                    patientIds.Contains(document.VaccinationRecord.PatientId))));

        if (request.PatientId.HasValue)
        {
            if (!patientIds.Contains(request.PatientId.Value))
            {
                throw new ForbiddenException(
                    $"You cannot access patient '{request.PatientId.Value}'.");
            }

            query = query.Where(document =>
                document.PatientId == request.PatientId.Value ||
                (document.VaccinationRecord != null &&
                    document.VaccinationRecord.PatientId == request.PatientId.Value));
        }

        if (request.VaccinationRecordId.HasValue)
        {
            query = query.Where(document =>
                document.VaccinationRecordId == request.VaccinationRecordId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (!TryParseDocumentType(request.Type, out var type))
            {
                throw new ValidationException(
                    $"Document type '{request.Type}' is invalid.");
            }

            query = query.Where(document => document.Type == type);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var documents = await query
            .OrderByDescending(document => document.CreatedAt)
            .ThenBy(document => document.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(document => ToResponse(document))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (totalCount + request.PageSize - 1) / request.PageSize;

        return new PagedResponse<DocumentResponse>(
            documents,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<DocumentResponse> GetDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentEntityAsync(
            documentId,
            asNoTracking: true,
            cancellationToken);

        return ToResponse(document);
    }

    public async Task<DocumentResponse> GetMyDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetMyDocumentEntityAsync(
            documentId,
            cancellationToken);

        return ToResponse(document);
    }

    public async Task<DocumentResponse> UploadDocumentAsync(
        UploadDocumentRequest request,
        Stream content,
        string fileName,
        string contentType,
        long sizeInBytes,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = ResolveHospitalId();
        EnsureHospitalAccess(hospitalId);

        if (!request.PatientId.HasValue && !request.VaccinationRecordId.HasValue)
        {
            throw new ValidationException(
                "PatientId or VaccinationRecordId is required.");
        }

        if (!TryParseDocumentType(request.Type, out var type))
        {
            throw new ValidationException(
                $"Document type '{request.Type}' is invalid.");
        }

        ValidateFile(fileName, contentType, sizeInBytes);

        await ValidateRelatedEntitiesAsync(
            hospitalId,
            request.PatientId,
            request.VaccinationRecordId,
            cancellationToken);

        var storageResult = await _documentStorageService.SaveAsync(
            content,
            fileName,
            contentType,
            cancellationToken);

        var document = new Document
        {
            HospitalId = hospitalId,
            PatientId = request.PatientId,
            VaccinationRecordId = request.VaccinationRecordId,
            FileName = Path.GetFileName(fileName),
            BlobName = storageResult.BlobName,
            ContentType = contentType,
            SizeInBytes = sizeInBytes,
            Type = type,
            Status = EntityStatus.Active,
            CreatedBy = _currentUser.UserId.ToString()
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Document {DocumentId} uploaded for hospital {HospitalId}.",
            document.Id,
            document.HospitalId);

        return ToResponse(document);
    }

    public async Task<DocumentStorageFile> DownloadDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentEntityAsync(
            documentId,
            asNoTracking: true,
            cancellationToken);

        return await _documentStorageService.OpenReadAsync(
            document.BlobName,
            document.FileName,
            document.ContentType,
            cancellationToken);
    }

    public async Task<DocumentStorageFile> DownloadMyDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetMyDocumentEntityAsync(
            documentId,
            cancellationToken);

        return await _documentStorageService.OpenReadAsync(
            document.BlobName,
            document.FileName,
            document.ContentType,
            cancellationToken);
    }

    public async Task DeleteDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentEntityAsync(
            documentId,
            asNoTracking: false,
            cancellationToken);

        document.IsDeleted = true;
        document.Status = EntityStatus.Inactive;
        document.UpdatedAt = DateTime.UtcNow;
        document.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Document {DocumentId} deleted for hospital {HospitalId}.",
            document.Id,
            document.HospitalId);
    }

    private Guid ResolveHospitalId()
    {
        if (!_currentUser.HospitalId.HasValue || IsPlatformAdmin())
        {
            throw new ForbiddenException("Hospital access is required.");
        }

        return _currentUser.HospitalId.Value;
    }

    private void EnsureHospitalAccess(Guid hospitalId)
    {
        if (!_currentUser.HospitalId.HasValue ||
            _currentUser.HospitalId.Value != hospitalId ||
            IsPlatformAdmin())
        {
            throw new ForbiddenException(
                $"You cannot access hospital '{hospitalId}'.");
        }
    }

    private bool IsPlatformAdmin()
    {
        return string.Equals(
            _currentUser.Role,
            Role.PlatformAdmin.ToString(),
            StringComparison.Ordinal);
    }

    private void EnsurePatientRole()
    {
        if (!string.Equals(
                _currentUser.Role,
                Role.Patient.ToString(),
                StringComparison.Ordinal))
        {
            throw new ForbiddenException("Patient access is required.");
        }
    }

    private async Task<List<Guid>> GetAccessiblePatientIdsAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.PatientPortalAccesses
            .AsNoTracking()
            .Where(access =>
                access.UserId == _currentUser.UserId &&
                !access.IsDeleted &&
                !access.Patient.IsDeleted &&
                access.Patient.Status == EntityStatus.Active)
            .Select(access => access.PatientId)
            .ToListAsync(cancellationToken);
    }

    private async Task<Document> GetMyDocumentEntityAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        EnsurePatientRole();

        var patientIds = await GetAccessiblePatientIdsAsync(cancellationToken);

        var document = await _dbContext.Documents
            .AsNoTracking()
            .Include(document => document.VaccinationRecord)
            .FirstOrDefaultAsync(
                document =>
                    document.Id == documentId &&
                    !document.IsDeleted &&
                    document.Status == EntityStatus.Active &&
                    ((document.PatientId.HasValue &&
                        patientIds.Contains(document.PatientId.Value)) ||
                    (document.VaccinationRecord != null &&
                        patientIds.Contains(document.VaccinationRecord.PatientId))),
                cancellationToken);

        if (document is null)
        {
            throw new NotFoundException("Document", documentId);
        }

        return document;
    }

    private async Task<Document> GetDocumentEntityAsync(
        Guid documentId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = asNoTracking
            ? _dbContext.Documents.AsNoTracking()
            : _dbContext.Documents;

        var document = await query.FirstOrDefaultAsync(
            document => document.Id == documentId && !document.IsDeleted,
            cancellationToken);

        if (document is null)
        {
            throw new NotFoundException("Document", documentId);
        }

        EnsureHospitalAccess(document.HospitalId);

        return document;
    }

    private async Task ValidateRelatedEntitiesAsync(
        Guid hospitalId,
        Guid? patientId,
        Guid? vaccinationRecordId,
        CancellationToken cancellationToken)
    {
        if (patientId.HasValue)
        {
            var patientExists = await _dbContext.Patients
                .AsNoTracking()
                .AnyAsync(
                    patient =>
                        patient.Id == patientId.Value &&
                        patient.HospitalId == hospitalId &&
                        !patient.IsDeleted,
                    cancellationToken);

            if (!patientExists)
            {
                throw new NotFoundException("Patient", patientId.Value);
            }
        }

        if (vaccinationRecordId.HasValue)
        {
            var record = await _dbContext.VaccinationRecords
                .AsNoTracking()
                .Where(record =>
                    record.Id == vaccinationRecordId.Value &&
                    record.HospitalId == hospitalId &&
                    !record.IsDeleted)
                .Select(record => new
                {
                    record.PatientId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (record is null)
            {
                throw new NotFoundException(
                    "Vaccination record",
                    vaccinationRecordId.Value);
            }

            if (patientId.HasValue && record.PatientId != patientId.Value)
            {
                throw new ValidationException(
                    "PatientId does not match the vaccination record patient.");
            }
        }
    }

    private static void ValidateFile(
        string fileName,
        string contentType,
        long sizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("File name is required.");
        }

        if (sizeInBytes <= 0)
        {
            throw new ValidationException("File cannot be empty.");
        }

        if (sizeInBytes > MaxFileSizeInBytes)
        {
            throw new ValidationException("File size cannot exceed 10 MB.");
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ValidationException(
                "Only PDF, JPEG, and PNG files are allowed.");
        }
    }

    private static bool TryParseDocumentType(
        string type,
        out DocumentType value)
    {
        return Enum.TryParse(type, ignoreCase: true, out value) &&
            Enum.IsDefined(value);
    }

    private static bool TryParseEntityStatus(
        string status,
        out EntityStatus value)
    {
        return Enum.TryParse(status, ignoreCase: true, out value) &&
            Enum.IsDefined(value);
    }

    private static DocumentResponse ToResponse(Document document)
    {
        return new DocumentResponse(
            document.Id,
            document.HospitalId,
            document.PatientId,
            document.VaccinationRecordId,
            document.FileName,
            document.ContentType,
            document.SizeInBytes,
            document.Type.ToString(),
            document.Status.ToString(),
            document.CreatedAt,
            document.UpdatedAt);
    }
}
