namespace VaccineTracker.Application.Users;

public enum UserOperationStatus
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    InvalidRole,
    InvalidGender,
    InvalidHospital
}
