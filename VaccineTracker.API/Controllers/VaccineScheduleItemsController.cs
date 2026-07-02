using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccineScheduleItems;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/vaccine-schedule-items")]
public sealed class VaccineScheduleItemsController : ControllerBase
{
    private readonly IVaccineScheduleItemsService _scheduleItemsService;

    public VaccineScheduleItemsController(
        IVaccineScheduleItemsService scheduleItemsService)
    {
        _scheduleItemsService = scheduleItemsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VaccineScheduleItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<VaccineScheduleItemResponse>>> GetScheduleItems(
        [FromQuery] GetVaccineScheduleItemsRequest request,
        CancellationToken cancellationToken)
    {
        var items = await _scheduleItemsService.GetScheduleItemsAsync(
            request,
            cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VaccineScheduleItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineScheduleItemResponse>> GetScheduleItem(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await _scheduleItemsService.GetScheduleItemAsync(
            id,
            cancellationToken);

        return Ok(item);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPost]
    [ProducesResponseType(typeof(VaccineScheduleItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccineScheduleItemResponse>> CreateScheduleItem(
        CreateVaccineScheduleItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _scheduleItemsService.CreateScheduleItemAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetScheduleItem),
            new { id = item.Id },
            item);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VaccineScheduleItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccineScheduleItemResponse>> UpdateScheduleItem(
        Guid id,
        UpdateVaccineScheduleItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _scheduleItemsService.UpdateScheduleItemAsync(
            id,
            request,
            cancellationToken);

        return Ok(item);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(VaccineScheduleItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineScheduleItemResponse>> ActivateScheduleItem(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await _scheduleItemsService.ActivateScheduleItemAsync(
            id,
            cancellationToken);

        return Ok(item);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(VaccineScheduleItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineScheduleItemResponse>> DeactivateScheduleItem(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await _scheduleItemsService.DeactivateScheduleItemAsync(
            id,
            cancellationToken);

        return Ok(item);
    }
}
