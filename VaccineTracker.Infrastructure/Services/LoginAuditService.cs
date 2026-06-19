using VaccineTracker.Application.Interfaces;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Infrastructure.Persistence;
namespace VaccineTracker.Infrastructure.Services;

public class LoginAuditService : ILoginAuditService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly IRequestContext _requestContext;
    private readonly ICurrentUser _currentUser;

    public LoginAuditService(VaccineTrackerDbContext dbContext, IRequestContext requestContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
        _currentUser = currentUser;
    }

    public async Task RecordLoginAsync(Guid? userId, string userName, bool isSuccess, string? failureReason, CancellationToken cancellationToken = default)
    {
        var loginAudit = new LoginAudit
        {
            UserId = userId,
            Username = userName,
            AttemptedAtUtc = DateTime.UtcNow,
            IsSuccessful = isSuccess,
            FailureReason = failureReason,
            CorrelationId = _requestContext.CorrelationId,
            IpAddress = _requestContext.IpAddress,
            UserAgent = _requestContext.UserAgent,
           
        };

        _dbContext.LoginAudits.Add(loginAudit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}