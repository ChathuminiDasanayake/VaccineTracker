using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccineManufacturers;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/vaccine-manufacturers")]
public sealed class VaccineManufacturersController : ControllerBase
{
    private readonly IVaccineManufacturersService _manufacturersService;

    public VaccineManufacturersController(
        IVaccineManufacturersService manufacturersService)
    {
        _manufacturersService = manufacturersService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VaccineManufacturerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<VaccineManufacturerResponse>>> GetManufacturers(
        [FromQuery] GetVaccineManufacturersRequest request,
        CancellationToken cancellationToken)
    {
        var manufacturers = await _manufacturersService.GetManufacturersAsync(
            request,
            cancellationToken);

        return Ok(manufacturers);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VaccineManufacturerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineManufacturerResponse>> GetManufacturer(
        Guid id,
        CancellationToken cancellationToken)
    {
        var manufacturer = await _manufacturersService.GetManufacturerAsync(
            id,
            cancellationToken);

        return Ok(manufacturer);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPost]
    [ProducesResponseType(typeof(VaccineManufacturerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccineManufacturerResponse>> CreateManufacturer(
        CreateVaccineManufacturerRequest request,
        CancellationToken cancellationToken)
    {
        var manufacturer = await _manufacturersService.CreateManufacturerAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetManufacturer),
            new { id = manufacturer.Id },
            manufacturer);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VaccineManufacturerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccineManufacturerResponse>> UpdateManufacturer(
        Guid id,
        UpdateVaccineManufacturerRequest request,
        CancellationToken cancellationToken)
    {
        var manufacturer = await _manufacturersService.UpdateManufacturerAsync(
            id,
            request,
            cancellationToken);

        return Ok(manufacturer);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(VaccineManufacturerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineManufacturerResponse>> ActivateManufacturer(
        Guid id,
        CancellationToken cancellationToken)
    {
        var manufacturer = await _manufacturersService.ActivateManufacturerAsync(
            id,
            cancellationToken);

        return Ok(manufacturer);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(VaccineManufacturerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineManufacturerResponse>> DeactivateManufacturer(
        Guid id,
        CancellationToken cancellationToken)
    {
        var manufacturer = await _manufacturersService.DeactivateManufacturerAsync(
            id,
            cancellationToken);

        return Ok(manufacturer);
    }
}
