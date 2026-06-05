using System;
using System.Collections.Generic;

namespace ATLAS.Application.DTOs
{
    public class ApplicationSummaryDto
    {
        public Guid Id { get; set; }
        public string ApplicationNumber { get; set; }
        public int Status { get; set; }
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
    }

    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string BlobUrl { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
    }

    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid OfficerId { get; set; }
        public int Decision { get; set; }
        public string? ReasonCode { get; set; }
        public string? Comments { get; set; }
        public DateTime ReviewedDate { get; set; }
        public bool IsVisibleToCitizen { get; set; }
    }

    public class PermitTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Department { get; set; }
        public bool IsActive { get; set; }
    }

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Guid RecordId { get; set; }
        public string? Details { get; set; }
    }
}
