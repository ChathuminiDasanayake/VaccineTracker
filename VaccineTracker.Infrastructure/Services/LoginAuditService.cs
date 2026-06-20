using VaccineTracker.Application.Interfaces;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class LoginAuditService : ILoginAuditService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly IRequestContext _requestContext;

    public LoginAuditService(
        VaccineTrackerDbContext dbContext,
        IRequestContext requestContext)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
    }

    public async Task RecordLoginAsync(
        Guid? userId,
        string username,
        bool isSuccessful,
        string? failureReason,
        CancellationToken cancellationToken = default)
    {
        var loginAudit = new LoginAudit
        {
            UserId = userId,
            Username = Truncate(username, 100) ?? string.Empty,
            AttemptedAtUtc = DateTime.UtcNow,
            IsSuccessful = isSuccessful,
            FailureReason = Truncate(failureReason, 200),
            CorrelationId = Truncate(_requestContext.CorrelationId, 100),
            IpAddress = Truncate(_requestContext.IpAddress, 45),
            UserAgent = Truncate(_requestContext.UserAgent, 500)
        };

        _dbContext.LoginAudits.Add(loginAudit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maximumLength)
    {
        return value?.Length > maximumLength
            ? value[..maximumLength]
            : value;
    }
}
