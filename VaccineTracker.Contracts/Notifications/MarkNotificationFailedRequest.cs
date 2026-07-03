using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.Notifications;

public sealed record MarkNotificationFailedRequest(
    [MaxLength(1000)]
    string? FailureReason);
