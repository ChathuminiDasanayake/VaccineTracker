using System;
using System.Collections.Generic;
using VaccineTracker.Domain.Common;
using VaccineTracker.Domain.Enums;

namespace VaccineTracker.Domain.Entities
{
    public sealed class User : BaseAuditableEntity
    {
        public string Username { get; set; } = string.Empty;

        public string NormalizedUsername { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string NormalizedEmail { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public Gender? Gender { get; set; }

        public Guid? HospitalId { get; set; }

        public Hospital? Hospital { get; set; }

        public string? PhoneNumber { get; set; }

        public EntityStatus Status { get; set; } = EntityStatus.Active;

        public DateTime? LastLoginAt { get; set; }

        public string? EmployeeId { get; set; }

        public List<Role> Roles { get; set; } = [];
    }
}
