using VaccineTracker.Contracts.Auth;

namespace VaccineTracker.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
