using VaccineTracker.Domain.Common;
using VaccineTracker.Domain.Enums;

namespace VaccineTracker.Domain.Entities
{
    public sealed class VaccinationRecord : BaseAuditableEntity
    {
        public Guid PatientId { get; set; }

        public Guid HospitalId { get; set; }

        public Guid VaccineId { get; set; }

        public Guid? VaccineScheduleItemId { get; set; }

        public int DoseNumber { get; set; }

        public DateOnly AdministeredDate { get; set; }

        public Guid AdministeredByUserId { get; set; }

        public string? BatchNumber { get; set; }

        public string? Notes { get; set; }

        public Patient Patient { get; set; } = null!;

        public Hospital Hospital { get; set; } = null!;

        public Vaccine Vaccine { get; set; } = null!;

        public VaccinationRecordStatus Status { get; set; } = VaccinationRecordStatus.Administered;

        public VaccineScheduleItem? VaccineScheduleItem { get; set; }
    }
}