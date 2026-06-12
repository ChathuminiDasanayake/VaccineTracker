using System;
using VaccineTracker.Domain.Common;

namespace VaccineTracker.Domain.Entities
{
    public sealed class Location : BaseAuditableEntity
    {
        public string? Street { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? PostalCode { get; set; }

        public string? Country { get; set; }
    }
}
