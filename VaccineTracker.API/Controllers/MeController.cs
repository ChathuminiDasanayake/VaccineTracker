using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Documents;
using VaccineTracker.Contracts.VaccinationRecords;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.PatientSelfService)]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    private readonly IDocumentsService _documentsService;
    private readonly IVaccinationRecordsService _vaccinationRecordsService;

    public MeController(
        IDocumentsService documentsService,
        IVaccinationRecordsService vaccinationRecordsService)
    {
        _documentsService = documentsService;
        _vaccinationRecordsService = vaccinationRecordsService;
    }

    [HttpGet("documents")]
    [ProducesResponseType(typeof(PagedResponse<DocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<DocumentResponse>>> GetMyDocuments(
        [FromQuery] GetDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        var documents = await _documentsService.GetMyDocumentsAsync(
            request,
            cancellationToken);

        return Ok(documents);
    }

    [HttpGet("documents/{id:guid}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> GetMyDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _documentsService.GetMyDocumentAsync(
            id,
            cancellationToken);

        return Ok(document);
    }

    [HttpGet("documents/{id:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadMyDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _documentsService.DownloadMyDocumentAsync(
            id,
            cancellationToken);

        return File(
            document.Content,
            document.ContentType,
            document.FileName);
    }

    [HttpGet("vaccination-records")]
    [ProducesResponseType(typeof(PagedResponse<VaccinationRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<VaccinationRecordResponse>>> GetMyVaccinationRecords(
        [FromQuery] GetVaccinationRecordsRequest request,
        CancellationToken cancellationToken)
    {
        var records = await _vaccinationRecordsService.GetMyRecordsAsync(
            request,
            cancellationToken);

        return Ok(records);
    }

    [HttpGet("vaccination-records/{id:guid}")]
    [ProducesResponseType(typeof(VaccinationRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccinationRecordResponse>> GetMyVaccinationRecord(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await _vaccinationRecordsService.GetMyRecordAsync(
            id,
            cancellationToken);

        return Ok(record);
    }
}
