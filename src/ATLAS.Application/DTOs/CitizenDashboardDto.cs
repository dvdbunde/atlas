using System;
using ATLAS.Domain.Enums;

namespace ATLAS.Application.DTOs
{
    public class CitizenDashboardDto
    {
        public Guid ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string PermitTypeName { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
