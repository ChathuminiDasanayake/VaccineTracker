using Microsoft.AspNetCore.Mvc;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Hospitals;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HospitalsController : ControllerBase
{
    private readonly IHospitalsService _hospitalsService;

    public HospitalsController(IHospitalsService hospitalsService)
    {
        _hospitalsService = hospitalsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HospitalResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HospitalResponse>>> GetHospitals(
        CancellationToken cancellationToken)
    {
        var hospitals = await _hospitalsService.GetHospitalsAsync(cancellationToken);

        return Ok(hospitals);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HospitalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HospitalResponse>> GetHospital(
        Guid id,
        CancellationToken cancellationToken)
    {
        var hospital = await _hospitalsService.GetHospitalAsync(id, cancellationToken);

        return hospital is null ? NotFound() : Ok(hospital);
    }

    [HttpPost]
    [ProducesResponseType(typeof(HospitalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HospitalResponse>> CreateHospital(
        CreateHospitalRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Hospital name is required.");
        }

        var hospital = await _hospitalsService.CreateHospitalAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetHospital), new { id = hospital.Id }, hospital);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(HospitalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HospitalResponse>> UpdateHospital(
        Guid id,
        UpdateHospitalRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Hospital name is required.");
        }

        var hospital = await _hospitalsService.UpdateHospitalAsync(id, request, cancellationToken);

        return hospital is null ? NotFound() : Ok(hospital);
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateHospital(Guid id, CancellationToken cancellationToken)
    {
        var hospitalExists = await _hospitalsService.ActivateHospitalAsync(id, cancellationToken);

        return hospitalExists ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateHospital(Guid id, CancellationToken cancellationToken)
    {
        var hospitalExists = await _hospitalsService.DeactivateHospitalAsync(id, cancellationToken);

        return hospitalExists ? NoContent() : NotFound();
    }
}
