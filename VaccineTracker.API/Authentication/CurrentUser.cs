using System.Security.Claims;
using VaccineTracker.Application.Authentication;
using VaccineTracker.Application.Interfaces;

namespace VaccineTracker.API.Authentication;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId => GetRequiredGuidClaim(JwtClaimNames.UserId);

    public Guid? HospitalId => GetOptionalGuidClaim(JwtClaimNames.HospitalId);

    public string Email => GetClaimValue(ClaimTypes.Email);

    public string Role => GetClaimValue(JwtClaimNames.Role);

    private string GetClaimValue(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(claimType) ?? string.Empty;
    }

    private Guid GetRequiredGuidClaim(string claimType)
    {
        var claimValue = GetClaimValue(claimType);

        return Guid.TryParse(claimValue, out var value)
            ? value
            : throw new InvalidOperationException($"Required claim '{claimType}' is missing or invalid.");
    }

    private Guid? GetOptionalGuidClaim(string claimType)
    {
        var claimValue = GetClaimValue(claimType);

        return Guid.TryParse(claimValue, out var value)
            ? value
            : null;
    }
}
