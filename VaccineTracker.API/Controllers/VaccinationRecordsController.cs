using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccinationRecords;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.HospitalStaff)]
[Route("api/vaccination-records")]
public sealed class VaccinationRecordsController : ControllerBase
{
    private readonly IVaccinationRecordsService _recordsService;

    public VaccinationRecordsController(IVaccinationRecordsService recordsService)
    {
        _recordsService = recordsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VaccinationRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<VaccinationRecordResponse>>> GetRecords(
        [FromQuery] GetVaccinationRecordsRequest request,
        CancellationToken cancellationToken)
    {
        var records = await _recordsService.GetRecordsAsync(
            request,
            cancellationToken);

        return Ok(records);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VaccinationRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccinationRecordResponse>> GetRecord(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await _recordsService.GetRecordAsync(
            id,
            cancellationToken);

        return Ok(record);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VaccinationRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccinationRecordResponse>> CreateRecord(
        CreateVaccinationRecordRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _recordsService.CreateRecordAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetRecord),
            new { id = record.Id },
            record);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VaccinationRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccinationRecordResponse>> UpdateRecord(
        Guid id,
        UpdateVaccinationRecordRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _recordsService.UpdateRecordAsync(
            id,
            request,
            cancellationToken);

        return Ok(record);
    }

    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(VaccinationRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccinationRecordResponse>> CancelRecord(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await _recordsService.CancelRecordAsync(
            id,
            cancellationToken);

        return Ok(record);
    }

    [HttpPatch("{id:guid}/entered-in-error")]
    [ProducesResponseType(typeof(VaccinationRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccinationRecordResponse>> MarkRecordEnteredInError(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await _recordsService.MarkRecordEnteredInErrorAsync(
            id,
            cancellationToken);

        return Ok(record);
    }
}
