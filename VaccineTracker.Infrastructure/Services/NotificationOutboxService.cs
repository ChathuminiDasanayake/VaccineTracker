using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Notifications;
using VaccineTracker.Contracts.VaccinationRecords;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class NotificationOutboxService : INotificationOutboxService
{
    private const int ReminderDaysBeforeDueDate = 7;

    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ILogger<NotificationOutboxService> _logger;

    public NotificationOutboxService(
        VaccineTrackerDbContext dbContext,
        ILogger<NotificationOutboxService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<NotificationOutboxResponse?> CreateVaccinationReminderAsync(
        NextVaccinationDueResponse nextDue,
        Guid? vaccinationRecordId,
        CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                patient =>
                    patient.Id == nextDue.PatientId &&
                    !patient.IsDeleted,
                cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException("Patient", nextDue.PatientId);
        }

        if (string.IsNullOrWhiteSpace(patient.Email))
        {
            _logger.LogInformation(
                "Vaccination reminder not created for patient {PatientId} because email is missing.",
                patient.Id);

            return null;
        }

        var existingNotification = await _dbContext.NotificationOutbox
            .AsNoTracking()
            .Include(notification => notification.Patient)
            .FirstOrDefaultAsync(
                notification =>
                    !notification.IsDeleted &&
                    notification.PatientId == nextDue.PatientId &&
                    notification.VaccineScheduleItemId == nextDue.VaccineScheduleItemId &&
                    notification.Type == NotificationType.VaccinationReminder &&
                    notification.Channel == NotificationChannel.Email &&
                    notification.Status != NotificationStatus.Sent &&
                    notification.Status != NotificationStatus.Cancelled,
                cancellationToken);

        if (existingNotification is not null)
        {
            _logger.LogInformation(
                "Vaccination reminder {NotificationId} already exists for patient {PatientId} and schedule item {ScheduleItemId}.",
                existingNotification.Id,
                nextDue.PatientId,
                nextDue.VaccineScheduleItemId);

            return ToResponse(existingNotification);
        }

        var sendAfterUtc = CalculateSendAfterUtc(nextDue.DueDate);
        var patientName = $"{patient.FirstName} {patient.LastName}".Trim();

        var notification = new NotificationOutbox
        {
            PatientId = nextDue.PatientId,
            VaccineScheduleItemId = nextDue.VaccineScheduleItemId,
            VaccinationRecordId = vaccinationRecordId,
            Type = NotificationType.VaccinationReminder,
            Channel = NotificationChannel.Email,
            Recipient = patient.Email.Trim(),
            Subject = $"Vaccination reminder: {nextDue.VaccineTypeName}",
            PayloadJson = JsonSerializer.Serialize(new
            {
                nextDue.PatientId,
                nextDue.PatientNumber,
                PatientName = patientName,
                nextDue.VaccineTypeId,
                nextDue.VaccineTypeName,
                nextDue.VaccineTypeCode,
                nextDue.VaccineScheduleItemId,
                nextDue.TargetGroup,
                nextDue.DoseNumber,
                nextDue.DueDate,
                nextDue.DaysUntilDue,
                nextDue.IsOverdue,
                nextDue.Description
            }),
            DueDate = nextDue.DueDate,
            SendAfterUtc = sendAfterUtc,
            Status = NotificationStatus.Pending
        };

        _dbContext.NotificationOutbox.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccination reminder {NotificationId} created for patient {PatientId}, due on {DueDate}.",
            notification.Id,
            notification.PatientId,
            nextDue.DueDate);

        return await GetNotificationAsync(notification.Id, cancellationToken);
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
                notification.DueDate,
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
            notification.DueDate,
            notification.SendAfterUtc,
            notification.Status.ToString(),
            notification.AttemptCount,
            notification.LastAttemptAtUtc,
            notification.SentAtUtc,
            notification.FailureReason,
            notification.CreatedAt,
            notification.UpdatedAt);
    }

    private static DateTime CalculateSendAfterUtc(DateOnly dueDate)
    {
        var reminderDate = dueDate.AddDays(-ReminderDaysBeforeDueDate)
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var now = DateTime.UtcNow;

        return reminderDate <= now
            ? now
            : reminderDate;
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
