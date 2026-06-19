using System;
using VaccineTracker.Domain.Common;
using VaccineTracker.Domain.Entities;

namespace VaccineTracker.Domain.Entities
{
    public sealed class LoginAudit 
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public bool IsSuccessful { get; set; }

        public string? FailureReason { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public string? CorrelationId { get; set; }

        public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;
    }
}