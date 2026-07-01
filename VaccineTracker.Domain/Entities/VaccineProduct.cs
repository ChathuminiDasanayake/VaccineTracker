using VaccineTracker.Domain.Common;
using VaccineTracker.Domain.Enums;

namespace VaccineTracker.Domain.Entities
{
    public sealed class VaccineProduct : BaseAuditableEntity
    {
        public Guid VaccineTypeId { get; set; }

        public Guid ManufacturerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public EntityStatus Status { get; set; } = EntityStatus.Active;

        public VaccineType VaccineType { get; set; } = null!;

        public VaccineManufacturer Manufacturer { get; set; } = null!;

        public ICollection<VaccinationRecord> VaccinationRecords { get; set; } = [];
    }
}
