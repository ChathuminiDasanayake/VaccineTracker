using VaccineTracker.Domain.Common;
using VaccineTracker.Domain.Enums;

namespace VaccineTracker.Domain.Entities
{
    public sealed class VaccineManufacturer : BaseAuditableEntity
    {
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        public EntityStatus Status { get; set; } = EntityStatus.Active;

        public ICollection<VaccineProduct> VaccineProducts { get; set; } = [];
    }
}
