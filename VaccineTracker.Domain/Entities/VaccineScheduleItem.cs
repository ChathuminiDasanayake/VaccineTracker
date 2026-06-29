using VaccineTracker.Domain.Common;
using VaccineTracker.Domain.Enums;

namespace VaccineTracker.Domain.Entities
{
    public sealed class VaccineScheduleItem : BaseAuditableEntity
    {
        public Guid VaccineId { get; set; }

        public VaccineTargetGroup TargetGroup { get; set; }

        public int? DueAgeInDays { get; set; }

        public int? MinimumAgeInYears { get; set; }

        public int? RepeatIntervalInDays { get; set; }

        public int DoseNumber { get; set; }

        public string? Description { get; set; }

        public EntityStatus Status { get; set; } = EntityStatus.Active;

        public Vaccine Vaccine { get; set; } = null!;
    }
}