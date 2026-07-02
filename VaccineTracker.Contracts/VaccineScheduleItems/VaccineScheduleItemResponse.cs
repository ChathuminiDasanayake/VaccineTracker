namespace VaccineTracker.Contracts.VaccineScheduleItems;

public sealed record VaccineScheduleItemResponse(
    Guid Id,
    Guid VaccineTypeId,
    string VaccineTypeName,
    string VaccineTypeCode,
    string TargetGroup,
    int? DueAgeInDays,
    int? MinimumAgeInYears,
    int? RepeatIntervalInDays,
    int DoseNumber,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
