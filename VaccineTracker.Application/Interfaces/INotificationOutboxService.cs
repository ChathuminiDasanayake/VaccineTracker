using VaccineTracker.Contracts.Notifications;

namespace VaccineTracker.Application.Interfaces;

public interface INotificationOutboxService
{
    Task<IReadOnlyList<NotificationOutboxResponse>> GetPendingAsync(
        GetPendingNotificationsRequest request,
        CancellationToken cancellationToken = default);

    Task<NotificationOutboxResponse> MarkProcessingAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<NotificationOutboxResponse> MarkSentAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<NotificationOutboxResponse> MarkFailedAsync(
        Guid notificationId,
        MarkNotificationFailedRequest request,
        CancellationToken cancellationToken = default);
}
