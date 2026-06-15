namespace VaccineTracker.Application.Users;

public sealed record UserOperationResult<T>(
    UserOperationStatus Status,
    T? Value = default);
