using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Documents;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.HospitalStaff)]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentsService _documentsService;
    private readonly IDocumentIntelligenceService _documentIntelligenceService;

    public DocumentsController(
        IDocumentsService documentsService,
        IDocumentIntelligenceService documentIntelligenceService)
    {
        _documentsService = documentsService;
        _documentIntelligenceService = documentIntelligenceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<DocumentResponse>>> GetDocuments(
        [FromQuery] GetDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        var documents = await _documentsService.GetDocumentsAsync(
            request,
            cancellationToken);

        return Ok(documents);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> GetDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _documentsService.GetDocumentAsync(
            id,
            cancellationToken);

        return Ok(document);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> UploadDocument(
        [FromForm] UploadDocumentForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null)
        {
            return BadRequest("File is required.");
        }

        await using var stream = form.File.OpenReadStream();

        var document = await _documentsService.UploadDocumentAsync(
            new UploadDocumentRequest
            {
                PatientId = form.PatientId,
                VaccinationRecordId = form.VaccinationRecordId,
                Type = form.Type
            },
            stream,
            form.File.FileName,
            form.File.ContentType,
            form.File.Length,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetDocument),
            new { id = document.Id },
            document);
    }

    [HttpGet("{id:guid}/download")]
    [Authorize(Policy = AuthorizationPolicies.ViewPatientSensitiveData)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _documentsService.DownloadDocumentAsync(
            id,
            cancellationToken);

        return File(
            document.Content,
            document.ContentType,
            document.FileName);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _documentsService.DeleteDocumentAsync(
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/analyze")]
    [Authorize(Policy = AuthorizationPolicies.ViewPatientSensitiveData)]
    [ProducesResponseType(typeof(DocumentExtractionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DocumentExtractionResponse>> AnalyzeDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        var extraction = await _documentIntelligenceService.AnalyzeDocumentAsync(
            id,
            cancellationToken);

        return Ok(extraction);
    }

    [HttpGet("{id:guid}/extractions")]
    [Authorize(Policy = AuthorizationPolicies.ViewPatientSensitiveData)]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentExtractionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DocumentExtractionResponse>>> GetDocumentExtractions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var extractions = await _documentIntelligenceService.GetDocumentExtractionsAsync(
            id,
            cancellationToken);

        return Ok(extractions);
    }
}

public sealed class UploadDocumentForm
{
    public Guid? PatientId { get; init; }

    public Guid? VaccinationRecordId { get; init; }

    public string Type { get; init; } = string.Empty;

    public IFormFile? File { get; init; }
}
