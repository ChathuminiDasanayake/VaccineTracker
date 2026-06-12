using System;

namespace VaccineTracker.Domain.Common
{
    public abstract class BaseAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? TenantId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
