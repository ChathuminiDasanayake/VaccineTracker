using System;
using System.Collections.Generic;
using VaccineTracker.Domain.Common;

namespace VaccineTracker.Domain.Entities
{
    public sealed class Hospital : BaseAuditableEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? RegistrationNumber { get; set; }

        public string? ContactPhone { get; set; }

        public string? ContactEmail { get; set; }

        public Guid? LocationId { get; set; }

        public Location? Location { get; set; }

        public string? OpeningHours { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Department>? Departments { get; set; }
    }
}
