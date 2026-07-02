using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.VaccineScheduleItems;

public sealed record CreateVaccineScheduleItemRequest(
    [Required]
    Guid VaccineTypeId,
    [Required]
    string TargetGroup,
    int? DueAgeInDays,
    int? MinimumAgeInYears,
    int? RepeatIntervalInDays,
    [Range(1, int.MaxValue)]
    int DoseNumber,
    [MaxLength(500)]
    string? Description);
