using VaccineTracker.Contracts.Notifications;
using VaccineTracker.Contracts.VaccinationRecords;

namespace VaccineTracker.Application.Interfaces;

public interface INotificationOutboxService
{
    Task<NotificationOutboxResponse?> CreateVaccinationReminderAsync(
        NextVaccinationDueResponse nextDue,
        Guid? vaccinationRecordId,
        CancellationToken cancellationToken = default);

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
