using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.VaccinationRecords;

public sealed record CreateVaccinationRecordRequest(
    [Required]
    Guid PatientId,
    [Required]
    Guid VaccineProductId,
    Guid? VaccineScheduleItemId,
    [Range(1, int.MaxValue)]
    int DoseNumber,
    DateOnly AdministeredDate,
    [MaxLength(100)]
    string? BatchNumber,
    [MaxLength(1000)]
    string? Notes);
