namespace VaccineTracker.Application.Interfaces;
public interface ILoginAuditService
{
    Task RecordLoginAsync(
        Guid? userId,
        string username,
        bool isSuccessful,
        string? failureReason,
        CancellationToken cancellationToken);
}