using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.Notifications;

public sealed class GetPendingNotificationsRequest
{
    [Range(1, 100)]
    public int Take { get; init; } = 20;
}
