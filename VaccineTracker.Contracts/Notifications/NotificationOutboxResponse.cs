namespace VaccineTracker.Contracts.Notifications;

public sealed record NotificationOutboxResponse(
    Guid Id,
    Guid PatientId,
    string PatientNumber,
    Guid? VaccineScheduleItemId,
    Guid? VaccinationRecordId,
    string Type,
    string Channel,
    string Recipient,
    string Subject,
    string PayloadJson,
    DateTime SendAfterUtc,
    string Status,
    int AttemptCount,
    DateTime? LastAttemptAtUtc,
    DateTime? SentAtUtc,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
