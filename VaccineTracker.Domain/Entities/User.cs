using System;
using System.Collections.Generic;
using VaccineTracker.Domain.Common;

namespace VaccineTracker.Domain.Entities
{
    public sealed class User : BaseAuditableEntity
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public Guid? HospitalId { get; set; }

        public Hospital? Hospital { get; set; }

        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        public string? EmployeeId { get; set; }

        public ICollection<string>? Roles { get; set; }
    }
}
