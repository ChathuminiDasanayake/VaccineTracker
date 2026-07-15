using VaccineTracker.Domain.Common;

namespace VaccineTracker.Domain.Entities;

public sealed class PatientPortalAccess : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public Guid PatientId { get; set; }

    public User User { get; set; } = null!;

    public Patient Patient { get; set; } = null!;
}
