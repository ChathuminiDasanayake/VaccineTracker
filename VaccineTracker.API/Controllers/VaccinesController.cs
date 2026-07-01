using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Vaccines;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class VaccinesController : ControllerBase
{
    private readonly IVaccinesService _vaccinesService;

    public VaccinesController(IVaccinesService vaccinesService)
    {
        _vaccinesService = vaccinesService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VaccineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<VaccineResponse>>> GetVaccines(
        [FromQuery] GetVaccinesRequest request,
        CancellationToken cancellationToken)
    {
        var vaccines = await _vaccinesService.GetVaccinesAsync(
            request,
            cancellationToken);

        return Ok(vaccines);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VaccineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineResponse>> GetVaccine(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vaccine = await _vaccinesService.GetVaccineAsync(
            id,
            cancellationToken);

        return Ok(vaccine);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPost]
    [ProducesResponseType(typeof(VaccineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccineResponse>> CreateVaccine(
        CreateVaccineRequest request,
        CancellationToken cancellationToken)
    {
        var vaccine = await _vaccinesService.CreateVaccineAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetVaccine),
            new { id = vaccine.Id },
            vaccine);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VaccineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccineResponse>> UpdateVaccine(
        Guid id,
        UpdateVaccineRequest request,
        CancellationToken cancellationToken)
    {
        var vaccine = await _vaccinesService.UpdateVaccineAsync(
            id,
            request,
            cancellationToken);

        return Ok(vaccine);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(VaccineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineResponse>> ActivateVaccine(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vaccine = await _vaccinesService.ActivateVaccineAsync(
            id,
            cancellationToken);

        return Ok(vaccine);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(VaccineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineResponse>> DeactivateVaccine(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vaccine = await _vaccinesService.DeactivateVaccineAsync(
            id,
            cancellationToken);

        return Ok(vaccine);
    }
}
