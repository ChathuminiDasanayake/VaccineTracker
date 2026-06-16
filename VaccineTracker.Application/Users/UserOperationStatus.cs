namespace VaccineTracker.Application.Users;

public enum UserOperationStatus
{
    Success,
    Unauthorized,
    NotFound,
    Forbidden,
    Conflict,
    InvalidRole,
    InvalidGender,
    InvalidHospital
}
