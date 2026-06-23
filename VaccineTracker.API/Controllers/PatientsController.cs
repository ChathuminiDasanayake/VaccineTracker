using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Patients;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.HospitalStaff)]
[Route("api/[controller]")]
public sealed class PatientsController : ControllerBase
{
    private readonly IPatientsService _patientsService;

    public PatientsController(IPatientsService patientsService)
    {
        _patientsService = patientsService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientSummaryResponse>> GetPatient(
        Guid id,
        CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetPatientAsync(
            id,
            cancellationToken);

        return Ok(patient);
    }

    [HttpGet("{id:guid}/details")]
    [Authorize(Policy = AuthorizationPolicies.ViewPatientSensitiveData)]
    [ProducesResponseType(typeof(PatientDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDetailsResponse>> GetPatientDetails(
        Guid id,
        CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetPatientDetailsAsync(
            id,
            cancellationToken);

        return Ok(patient);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PatientSummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PatientSummaryResponse>> CreatePatient(
        CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var patient = await _patientsService.CreatePatientAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetPatient),
            new { id = patient.Id },
            patient);
    }
}
