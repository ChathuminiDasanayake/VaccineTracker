using VaccineTracker.Domain.Common;
using VaccineTracker.Domain.Enums;

namespace VaccineTracker.Domain.Entities;

public sealed class NotificationOutbox : BaseAuditableEntity
{
    public Guid PatientId { get; set; }

    public Guid? VaccineScheduleItemId { get; set; }

    public Guid? VaccinationRecordId { get; set; }

    public NotificationType Type { get; set; }

    public NotificationChannel Channel { get; set; }

    public string Recipient { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime SendAfterUtc { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTime? LastAttemptAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public string? FailureReason { get; set; }

    public Patient Patient { get; set; } = null!;

    public VaccineScheduleItem? VaccineScheduleItem { get; set; }

    public VaccinationRecord? VaccinationRecord { get; set; }
}
