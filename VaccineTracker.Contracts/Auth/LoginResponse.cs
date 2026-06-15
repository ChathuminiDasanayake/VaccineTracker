namespace VaccineTracker.Contracts.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Username,
    IReadOnlyCollection<string> Roles,
    Guid? HospitalId);
