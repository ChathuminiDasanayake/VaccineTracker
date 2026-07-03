using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Notifications;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class NotificationOutboxService : INotificationOutboxService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ILogger<NotificationOutboxService> _logger;

    public NotificationOutboxService(
        VaccineTrackerDbContext dbContext,
        ILogger<NotificationOutboxService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationOutboxResponse>> GetPendingAsync(
        GetPendingNotificationsRequest request,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(request.Take, 1, 100);
        var now = DateTime.UtcNow;

        return await _dbContext.NotificationOutbox
            .AsNoTracking()
            .Include(notification => notification.Patient)
            .Where(notification =>
                !notification.IsDeleted &&
                notification.Status == NotificationStatus.Pending &&
                notification.SendAfterUtc <= now)
            .OrderBy(notification => notification.SendAfterUtc)
            .ThenBy(notification => notification.CreatedAt)
            .Take(take)
            .Select(notification => new NotificationOutboxResponse(
                notification.Id,
                notification.PatientId,
                notification.Patient.PatientNumber,
                notification.VaccineScheduleItemId,
                notification.VaccinationRecordId,
                notification.Type.ToString(),
                notification.Channel.ToString(),
                notification.Recipient,
                notification.Subject,
                notification.PayloadJson,
                notification.SendAfterUtc,
                notification.Status.ToString(),
                notification.AttemptCount,
                notification.LastAttemptAtUtc,
                notification.SentAtUtc,
                notification.FailureReason,
                notification.CreatedAt,
                notification.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationOutboxResponse> MarkProcessingAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await GetTrackedNotificationAsync(
            notificationId,
            cancellationToken);

        if (notification.Status is NotificationStatus.Sent or NotificationStatus.Cancelled)
        {
            throw new BusinessRuleException(
                $"Notification cannot be processed because it is {notification.Status}.");
        }

        notification.Status = NotificationStatus.Processing;
        notification.AttemptCount++;
        notification.LastAttemptAtUtc = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification {NotificationId} marked as processing.",
            notification.Id);

        return await GetNotificationAsync(notification.Id, cancellationToken);
    }

    public async Task<NotificationOutboxResponse> MarkSentAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await GetTrackedNotificationAsync(
            notificationId,
            cancellationToken);

        if (notification.Status == NotificationStatus.Sent)
        {
            throw new BusinessRuleException("Notification is already sent.");
        }

        if (notification.Status == NotificationStatus.Cancelled)
        {
            throw new BusinessRuleException("Cancelled notification cannot be sent.");
        }

        var now = DateTime.UtcNow;

        notification.Status = NotificationStatus.Sent;
        notification.SentAtUtc = now;
        notification.LastAttemptAtUtc ??= now;
        notification.UpdatedAt = now;
        notification.FailureReason = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification {NotificationId} marked as sent.",
            notification.Id);

        return await GetNotificationAsync(notification.Id, cancellationToken);
    }

    public async Task<NotificationOutboxResponse> MarkFailedAsync(
        Guid notificationId,
        MarkNotificationFailedRequest request,
        CancellationToken cancellationToken = default)
    {
        var notification = await GetTrackedNotificationAsync(
            notificationId,
            cancellationToken);

        if (notification.Status == NotificationStatus.Sent)
        {
            throw new BusinessRuleException("Sent notification cannot be marked as failed.");
        }

        if (notification.Status == NotificationStatus.Cancelled)
        {
            throw new BusinessRuleException("Cancelled notification cannot be marked as failed.");
        }

        var now = DateTime.UtcNow;

        notification.Status = NotificationStatus.Failed;
        notification.LastAttemptAtUtc = now;
        notification.UpdatedAt = now;
        notification.FailureReason = TrimOrNull(request.FailureReason);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification {NotificationId} marked as failed.",
            notification.Id);

        return await GetNotificationAsync(notification.Id, cancellationToken);
    }

    private async Task<NotificationOutbox> GetTrackedNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await _dbContext.NotificationOutbox
            .FirstOrDefaultAsync(
                notification => notification.Id == notificationId &&
                    !notification.IsDeleted,
                cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException("Notification", notificationId);
        }

        return notification;
    }

    private async Task<NotificationOutboxResponse> GetNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await _dbContext.NotificationOutbox
            .AsNoTracking()
            .Include(notification => notification.Patient)
            .FirstOrDefaultAsync(
                notification => notification.Id == notificationId &&
                    !notification.IsDeleted,
                cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException("Notification", notificationId);
        }

        return ToResponse(notification);
    }

    private static NotificationOutboxResponse ToResponse(NotificationOutbox notification)
    {
        return new NotificationOutboxResponse(
            notification.Id,
            notification.PatientId,
            notification.Patient.PatientNumber,
            notification.VaccineScheduleItemId,
            notification.VaccinationRecordId,
            notification.Type.ToString(),
            notification.Channel.ToString(),
            notification.Recipient,
            notification.Subject,
            notification.PayloadJson,
            notification.SendAfterUtc,
            notification.Status.ToString(),
            notification.AttemptCount,
            notification.LastAttemptAtUtc,
            notification.SentAtUtc,
            notification.FailureReason,
            notification.CreatedAt,
            notification.UpdatedAt);
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
