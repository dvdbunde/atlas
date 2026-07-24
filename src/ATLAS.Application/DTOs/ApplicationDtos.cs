using System;
using System.Collections.Generic;
using ATLAS.Domain.Entities;
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
        public string? AssignedOfficerName { get; set; }
        public Guid? AssignedOfficerId { get; set; }
        public string CitizenEmail { get; set; } = string.Empty;
        public string PermitTypeDescription { get; set; } = string.Empty;

        /// <summary>
        /// Current field values for this application.
        /// Key = FieldName (matches PermitField.Name), Value = entered value.
        /// </summary>
        public Dictionary<string, string> FieldValues { get; set; } = new();
    }

    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
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
        public int FieldCount { get; set; }
        public int DocumentRequirementCount { get; set; }
    }

    public class PermitTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public bool IsActive { get; set; }
        public List<FieldDefinitionDto> Fields { get; set; } = new();
        public List<FieldDefinitionDto> DocumentRequirements { get; set; } = new();
    }

    public class FieldDefinitionDto
    {
        public Guid Id { get; set; }
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

    /// <summary>
    /// Summary DTO for a single application on the officer dashboard.
    /// Contains only the fields needed for the summary card — never the full aggregate.
    /// </summary>
    public class OfficerDashboardDto
    {
        public Guid ApplicationId { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string PermitTypeName { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public string CitizenName { get; set; } = string.Empty;
        public DateTime? SubmittedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? AssignedOfficerName { get; set; }
        public Guid? AssignedOfficerId { get; set; }
        public int DocumentCount { get; set; }
        public bool AllRequiredDocumentsUploaded { get; set; }
    }

        /// <summary>Purpose-built read-only projection for the officer review page.</summary>
    /// <summary>
    /// Read-only projection for the officer review page.
    /// Composes general application information (via ApplicationDetailDto)
    /// with officer-specific review data.
    /// </summary>
    public class OfficerApplicationReviewDto
    {
        private ApplicationDetailDto? _application;

        /// <summary>General application information shared across all personas.</summary>
        public ApplicationDetailDto Application
        {
            get => _application ??= new ApplicationDetailDto();
            set => _application = value;
        }

        /// <summary>Requirement-centric document projection for the officer view.</summary>
        public List<OfficerDocumentRequirementDto> DocumentRequirements { get; set; } = new();

        /// <summary>Officer-specific field values with permit metadata (labels, types).</summary>
        public List<OfficerFieldValueDto> FieldValues { get; set; } = new();

        /// <summary>Officer-specific review projection.</summary>
        public List<OfficerReviewDto> Reviews { get; set; } = new();

        /// <summary>Officer-specific document list for backward compatibility.</summary>
        public List<OfficerDocumentDto> Documents { get; set; } = new();

        // ── Convenience forwarders (delegate to Application) ──
        public Guid ApplicationId { get => Application.Id; set => Application.Id = value; }
        public string ApplicationNumber { get => Application.ApplicationNumber; set => Application.ApplicationNumber = value; }
        public ApplicationStatus Status { get => Application.Status; set => Application.Status = value; }
        public string PermitTypeName { get => Application.PermitTypeName; set => Application.PermitTypeName = value; }
        public string PermitTypeDescription { get => Application.PermitTypeDescription; set => Application.PermitTypeDescription = value; }
        public DateTime? SubmittedDate { get => Application.SubmittedDate; set => Application.SubmittedDate = value; }
        public DateTime? LastUpdated { get => Application.ReviewedDate ?? Application.SubmittedDate; set { } }
        public Guid CitizenId { get => Application.CitizenId; set => Application.CitizenId = value; }
        public string CitizenName { get => Application.CitizenName; set => Application.CitizenName = value; }
        public string CitizenEmail { get => Application.CitizenEmail; set => Application.CitizenEmail = value; }
        public string? AssignedOfficerName { get => Application.AssignedOfficerName; set => Application.AssignedOfficerName = value; }
        public Guid? AssignedOfficerId { get => Application.AssignedOfficerId; set => Application.AssignedOfficerId = value; }
        public string CitizenNotes { get => Application.CitizenNotes; set => Application.CitizenNotes = value; }
    }
    
    public class OfficerFieldValueDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public FieldType FieldType { get; set; }
    }
    
    public class OfficerDocumentRequirementDto
    {
        public string DocumentType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsSatisfied { get; set; }
        public List<OfficerDocumentDto> UploadedDocuments { get; set; } = new();
    }
    
    public class OfficerDocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
    }
    
    public class OfficerReviewDto
    {
        public Guid Id { get; set; }
        public Guid OfficerId { get; set; }
        public ReviewDecision Decision { get; set; }
        public string? ReasonCode { get; set; }
        public string Comments { get; set; } = string.Empty;
        public DateTime ReviewedDate { get; set; }
    }

    /// <summary>
    /// A single chronological activity entry for an application.
    /// Projected from existing domain data — never a source of truth.
    /// </summary>
    public class ApplicationActivityDto
    {
        public DateTime Timestamp { get; init; }
        public string ActivityType { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? PerformedBy { get; init; }
        public string? PerformedByRole { get; init; }
    }

    /// <summary>
    /// Lightweight projection of a User for list/table rendering.
    /// The User aggregate is a synchronized, read-only projection of an Entra ID
    /// principal (see ADR-013); role and status are owned by Entra and must not be
    /// mutated locally.
    /// </summary>
    public class UserSummaryDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    /// <summary>
    /// Detailed projection of a User for the read-only detail view.
    /// Includes the most recent audit entries associated with the principal
    /// (projected from the audit log; read-only).
    /// </summary>
    public class UserDetailDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public IReadOnlyList<AuditLogDto> RecentAuditEntries { get; set; } = Array.Empty<AuditLogDto>();
    }
}
