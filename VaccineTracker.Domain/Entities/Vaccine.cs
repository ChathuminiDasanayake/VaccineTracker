using VaccineTracker.Domain.Common;
using VaccineTracker.Domain.Enums;

namespace VaccineTracker.Domain.Entities
{
    public sealed class Vaccine : BaseAuditableEntity
    {
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public Guid ManufacturerId { get; set; }

        public string DiseaseTarget { get; set; } = string.Empty;

        public string? Description { get; set; }

        public EntityStatus Status { get; set; } = EntityStatus.Active;

        public VaccineManufacturer Manufacturer { get; set; } = null!;

        public ICollection<VaccineScheduleItem> ScheduleItems { get; set; } = [];

        public ICollection<VaccinationRecord> VaccinationRecords { get; set; } = [];
    }
}
