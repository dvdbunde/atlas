using System;
using System.Collections.Generic;
using ATLAS.Domain.Enums;

namespace ATLAS.Application.DTOs
{
    public class ApplicationSummaryDto
    {
        public Guid Id { get; set; }
        public string ApplicationNumber { get; set; }
        public ApplicationStatus Status { get; set; } 
        public DateTime? SubmittedDate { get; set; }
        public Guid CitizenId { get; set; }
        public Guid PermitTypeId { get; set; }
        // NEW: Missing fields from PRD
        public string? CitizenName { get; set; }
        public string? PermitTypeName { get; set; }
    }

    public class ApplicationDetailDto : ApplicationSummaryDto
    {
        public DateTime? ReviewedDate { get; set; }
        public string CitizenNotes { get; set; } = string.Empty;
        public string OfficerNotes { get; set; } = string.Empty;
        public List<DocumentDto> Documents { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
        // NEW: Missing fields from PRD
        public string? OfficerName { get; set; }

        /// <summary>
        /// Current field values for this application.
        /// Key = FieldName (matches PermitField.Name), Value = entered value.
        /// </summary>
        public Dictionary<string, string> FieldValues { get; set; } = new();
    }

    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
        public Guid UploadedById { get; set; }
    }

    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid OfficerId { get; set; }
        public ReviewDecision Decision { get; set; }
        public string? ReasonCode { get; set; }
        public string? Comments { get; set; }
        public DateTime ReviewedDate { get; set; }
        public bool IsVisibleToCitizen { get; set; }
    }

    public class PermitTypeSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public bool IsActive { get; set; }
    }

    public class PermitTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public bool IsActive { get; set; }
        public List<FieldDefinitionDto> Fields { get; set; } = new();
    }

    public class FieldDefinitionDto
    {
        public string Name { get; set; } = string.Empty;
        public FieldType Type { get; set; } 
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public List<string> Options { get; set; } = new();
        public string? AllowedExtensions { get; set; }
        public long? MaxFileSizeBytes { get; set; }
    }

    /// <summary>
    /// User DTO - represents a synchronized Entra ID principal
    /// Read-only representation for queries (no identity management)
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime? LastLoginDate { get; set; }
    }

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }

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
