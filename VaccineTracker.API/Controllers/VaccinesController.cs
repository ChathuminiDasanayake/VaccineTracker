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
            nameof(GetVaccines),
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
}
