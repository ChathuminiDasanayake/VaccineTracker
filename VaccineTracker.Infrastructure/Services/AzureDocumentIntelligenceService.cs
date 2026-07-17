using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Documents;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class AzureDocumentIntelligenceService : IDocumentIntelligenceService
{
    private const int MaxExtractedValueLength = 2000;

    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly DocumentIntelligenceSettings _settings;
    private readonly ILogger<AzureDocumentIntelligenceService> _logger;

    public AzureDocumentIntelligenceService(
        VaccineTrackerDbContext dbContext,
        ICurrentUser currentUser,
        IDocumentStorageService documentStorageService,
        IOptions<DocumentIntelligenceSettings> settings,
        ILogger<AzureDocumentIntelligenceService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _documentStorageService = documentStorageService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<DocumentExtractionResponse> AnalyzeDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        var document = await GetDocumentForHospitalAsync(
            documentId,
            asNoTracking: false,
            cancellationToken);

        document.ProcessingStatus = DocumentProcessingStatus.Processing;
        document.UpdatedAt = DateTime.UtcNow;
        document.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var documentFile = await _documentStorageService.OpenReadAsync(
                document.BlobName,
                document.FileName,
                document.ContentType,
                cancellationToken);
            await using var documentContent = documentFile.Content;

            using var memoryStream = new MemoryStream();
            await documentContent.CopyToAsync(
                memoryStream,
                cancellationToken);

            var client = new DocumentIntelligenceClient(
                new Uri(_settings.Endpoint),
                new AzureKeyCredential(_settings.Key));

            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                _settings.DefaultModelId,
                BinaryData.FromBytes(memoryStream.ToArray()),
                cancellationToken);

            var result = operation.Value;
            var fields = CreateFields(result);

            var extraction = new DocumentExtraction
            {
                DocumentId = document.Id,
                ModelId = _settings.DefaultModelId,
                RawResultJson = CreateRawResultJson(result, fields),
                OverallConfidence = CalculateOverallConfidence(fields),
                ProcessedAt = DateTimeOffset.UtcNow,
                CreatedBy = _currentUser.UserId.ToString(),
                Fields = fields
            };

            document.ProcessingStatus = DocumentProcessingStatus.ReviewRequired;
            document.UpdatedAt = DateTime.UtcNow;
            document.UpdatedBy = _currentUser.UserId.ToString();

            _dbContext.DocumentExtractions.Add(extraction);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Document {DocumentId} analyzed using model {ModelId}.",
                document.Id,
                extraction.ModelId);

            return ToResponse(extraction);
        }
        catch (Exception exception) when (
            exception is not BusinessRuleException &&
            exception is not OperationCanceledException)
        {
            document.ProcessingStatus = DocumentProcessingStatus.Failed;
            document.UpdatedAt = DateTime.UtcNow;
            document.UpdatedBy = _currentUser.UserId.ToString();

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                exception,
                "Document analysis failed for document {DocumentId}.",
                document.Id);

            throw new BusinessRuleException(
                "Document analysis failed. Check Azure Document Intelligence configuration and document format.");
        }
    }

    public async Task<IReadOnlyList<DocumentExtractionResponse>> GetDocumentExtractionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        await GetDocumentForHospitalAsync(
            documentId,
            asNoTracking: true,
            cancellationToken);

        var extractions = await _dbContext.DocumentExtractions
            .AsNoTracking()
            .Include(extraction => extraction.Fields)
            .Where(extraction =>
                extraction.DocumentId == documentId &&
                !extraction.IsDeleted)
            .OrderByDescending(extraction => extraction.ProcessedAt)
            .ThenBy(extraction => extraction.Id)
            .ToListAsync(cancellationToken);

        return extractions
            .Select(ToResponse)
            .ToList();
    }

    private async Task<Document> GetDocumentForHospitalAsync(
        Guid documentId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = asNoTracking
            ? _dbContext.Documents.AsNoTracking()
            : _dbContext.Documents;

        var document = await query.FirstOrDefaultAsync(
            document =>
                document.Id == documentId &&
                !document.IsDeleted &&
                document.Status == EntityStatus.Active,
            cancellationToken);

        if (document is null)
        {
            throw new NotFoundException("Document", documentId);
        }

        EnsureHospitalAccess(document.HospitalId);

        return document;
    }

    private void EnsureHospitalAccess(Guid hospitalId)
    {
        if (!_currentUser.HospitalId.HasValue ||
            _currentUser.HospitalId.Value != hospitalId ||
            string.Equals(
                _currentUser.Role,
                Role.PlatformAdmin.ToString(),
                StringComparison.Ordinal))
        {
            throw new ForbiddenException(
                $"You cannot access hospital '{hospitalId}'.");
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.Endpoint) ||
            string.IsNullOrWhiteSpace(_settings.Key) ||
            string.IsNullOrWhiteSpace(_settings.DefaultModelId))
        {
            throw new BusinessRuleException(
                "Document Intelligence settings must include Endpoint, Key, and DefaultModelId.");
        }
    }

    private static List<ExtractedDocumentField> CreateFields(AnalyzeResult result)
    {
        var fields = new List<ExtractedDocumentField>();

        foreach (var analyzedDocument in result.Documents)
        {
            foreach (var field in analyzedDocument.Fields)
            {
                fields.Add(new ExtractedDocumentField
                {
                    FieldName = field.Key,
                    ExtractedValue = Truncate(GetFieldValue(field.Value)),
                    Confidence = ToDecimal(field.Value.Confidence),
                    IsApproved = false
                });
            }
        }

        if (fields.Count == 0 && !string.IsNullOrWhiteSpace(result.Content))
        {
            fields.Add(new ExtractedDocumentField
            {
                FieldName = "Content",
                ExtractedValue = Truncate(result.Content),
                IsApproved = false
            });
        }

        return fields;
    }

    private static string? GetFieldValue(DocumentField field)
    {
        if (!string.IsNullOrWhiteSpace(field.Content))
        {
            return field.Content;
        }

        if (!string.IsNullOrWhiteSpace(field.ValueString))
        {
            return field.ValueString;
        }

        if (field.ValueDate.HasValue)
        {
            return field.ValueDate.Value.ToString("O");
        }

        if (field.ValueTime.HasValue)
        {
            return field.ValueTime.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(field.ValuePhoneNumber))
        {
            return field.ValuePhoneNumber;
        }

        if (field.ValueDouble.HasValue)
        {
            return field.ValueDouble.Value.ToString();
        }

        if (field.ValueInt64.HasValue)
        {
            return field.ValueInt64.Value.ToString();
        }

        if (field.ValueBoolean.HasValue)
        {
            return field.ValueBoolean.Value.ToString();
        }

        return null;
    }

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value) ||
            value.Length <= MaxExtractedValueLength)
        {
            return value;
        }

        return value[..MaxExtractedValueLength];
    }

    private static decimal? CalculateOverallConfidence(
        IReadOnlyCollection<ExtractedDocumentField> fields)
    {
        var confidenceValues = fields
            .Where(field => field.Confidence.HasValue)
            .Select(field => field.Confidence!.Value)
            .ToList();

        if (confidenceValues.Count == 0)
        {
            return null;
        }

        return Math.Round(confidenceValues.Average(), 4);
    }

    private static decimal? ToDecimal(float? value)
    {
        return value.HasValue
            ? Math.Round((decimal)value.Value, 4)
            : null;
    }

    private static string CreateRawResultJson(
        AnalyzeResult result,
        IReadOnlyCollection<ExtractedDocumentField> fields)
    {
        var rawResult = new
        {
            result.Content,
            PageCount = result.Pages.Count,
            Fields = fields.Select(field => new
            {
                field.FieldName,
                field.ExtractedValue,
                field.Confidence
            })
        };

        return JsonSerializer.Serialize(rawResult);
    }

    private static DocumentExtractionResponse ToResponse(
        DocumentExtraction extraction)
    {
        return new DocumentExtractionResponse(
            extraction.Id,
            extraction.DocumentId,
            extraction.ModelId,
            extraction.OverallConfidence,
            extraction.ProcessedAt,
            extraction.Fields
                .Where(field => !field.IsDeleted)
                .OrderBy(field => field.FieldName)
                .ThenBy(field => field.Id)
                .Select(field => new ExtractedDocumentFieldResponse(
                    field.Id,
                    field.FieldName,
                    field.ExtractedValue,
                    field.CorrectedValue,
                    field.Confidence,
                    field.IsApproved))
                .ToList());
    }
}
