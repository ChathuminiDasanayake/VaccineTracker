using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.Patients;

public sealed record UpdatePatientStatusRequest(
    [Required]
    string Status);
