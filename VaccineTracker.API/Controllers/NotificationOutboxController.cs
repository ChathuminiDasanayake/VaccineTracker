using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Notifications;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
[Route("api/notification-outbox")]
public sealed class NotificationOutboxController : ControllerBase
{
    private readonly INotificationOutboxService _notificationOutboxService;

    public NotificationOutboxController(
        INotificationOutboxService notificationOutboxService)
    {
        _notificationOutboxService = notificationOutboxService;
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationOutboxResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<NotificationOutboxResponse>>> GetPending(
        [FromQuery] GetPendingNotificationsRequest request,
        CancellationToken cancellationToken)
    {
        var notifications = await _notificationOutboxService.GetPendingAsync(
            request,
            cancellationToken);

        return Ok(notifications);
    }

    [HttpPatch("{id:guid}/processing")]
    [ProducesResponseType(typeof(NotificationOutboxResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationOutboxResponse>> MarkProcessing(
        Guid id,
        CancellationToken cancellationToken)
    {
        var notification = await _notificationOutboxService.MarkProcessingAsync(
            id,
            cancellationToken);

        return Ok(notification);
    }

    [HttpPatch("{id:guid}/sent")]
    [ProducesResponseType(typeof(NotificationOutboxResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationOutboxResponse>> MarkSent(
        Guid id,
        CancellationToken cancellationToken)
    {
        var notification = await _notificationOutboxService.MarkSentAsync(
            id,
            cancellationToken);

        return Ok(notification);
    }

    [HttpPatch("{id:guid}/failed")]
    [ProducesResponseType(typeof(NotificationOutboxResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationOutboxResponse>> MarkFailed(
        Guid id,
        MarkNotificationFailedRequest request,
        CancellationToken cancellationToken)
    {
        var notification = await _notificationOutboxService.MarkFailedAsync(
            id,
            request,
            cancellationToken);

        return Ok(notification);
    }
}
