namespace VaccineTracker.API.Authorization;

public static class AuthorizationPolicies
{
    public const string PlatformAdmin = nameof(PlatformAdmin);
    public const string HospitalAdmin = nameof(HospitalAdmin);
    public const string HospitalStaff = nameof(HospitalStaff);
    public const string ViewPatientSensitiveData = nameof(ViewPatientSensitiveData);
}
