using System;
using VaccineTracker.Domain.Common;

namespace VaccineTracker.Domain.Entities
{
    public sealed class Department : BaseAuditableEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Guid HospitalId { get; set; }

        public Hospital? Hospital { get; set; }

        public string? DepartmentCode { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
